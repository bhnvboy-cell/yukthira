using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.BackgroundServices;

/// <summary>
/// Pulls rates from the configured <see cref="IExchangeRateProvider"/> and upserts
/// yuktira_fi.exchange_rates rows (base -> currency) for every target tenant, each tenant
/// inside its own transaction. Registered as scoped; the background job creates a scope per run,
/// and ExchangeRateController triggers the very same code path for "Sync Now".
/// </summary>
public class ExchangeRateSyncService : IExchangeRateSyncService
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;
    private readonly IEnumerable<IExchangeRateProvider> _providers;
    private readonly ExchangeRateSyncOptions _options;
    private readonly IExchangeRateSyncStatusStore _status;
    private readonly ILogger<ExchangeRateSyncService> _logger;

    public ExchangeRateSyncService(
        YuktiraDbContext db,
        ITenantContext tenant,
        IAuditService audit,
        IEnumerable<IExchangeRateProvider> providers,
        IOptions<ExchangeRateSyncOptions> options,
        IExchangeRateSyncStatusStore status,
        ILogger<ExchangeRateSyncService> logger)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
        _providers = providers;
        _options = options.Value;
        _status = status;
        _logger = logger;
    }

    public async Task<ExchangeRateSyncResultDto> SyncAsync(CancellationToken ct = default)
    {
        var baseCurrency = string.IsNullOrWhiteSpace(_options.BaseCurrency) ? "EUR" : _options.BaseCurrency.Trim().ToUpperInvariant();

        var result = new ExchangeRateSyncResultDto
        {
            StartedAt = DateTime.UtcNow,
            Provider = _options.Provider,
            BaseCurrency = baseCurrency
        };

        if (!_options.Enabled)
        {
            result.Errors.Add("Exchange rate sync is disabled by configuration (ExchangeRateSync:Enabled).");
            result.FinishedAt = DateTime.UtcNow;
            return result;
        }

        // Atomic claim so the background job and a manual "Sync Now" can never run together.
        if (!_status.TryBeginRun())
        {
            result.Errors.Add("An exchange rate sync run is already in progress.");
            result.FinishedAt = DateTime.UtcNow;
            return result;
        }

        try
        {
            await SyncCoreAsync(result, baseCurrency, ct);
        }
        catch (OperationCanceledException)
        {
            result.Errors.Add("The exchange rate sync run was cancelled.");
            _logger.LogInformation("Exchange rate sync run cancelled");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Exchange rate sync failed: {ex.Message}");
            _logger.LogError(ex, "Exchange rate sync failed");
        }
        finally
        {
            result.FinishedAt = DateTime.UtcNow;
            result.Success = result.Errors.Count == 0 && result.RatesFetched > 0;
            _status.EndRun(result);
        }

        return result;
    }

    private async Task SyncCoreAsync(ExchangeRateSyncResultDto result, string baseCurrency, CancellationToken ct)
    {
        var provider = _providers.FirstOrDefault(p => string.Equals(p.Name, _options.Provider, StringComparison.OrdinalIgnoreCase));
        if (provider == null)
        {
            var registered = string.Join(", ", _providers.Select(p => p.Name));
            result.Errors.Add($"No exchange rate provider named '{_options.Provider}' is registered (available: {registered}).");
            return;
        }

        result.Provider = provider.Name;

        var fetched = await provider.GetLatestRatesAsync(baseCurrency, ct);
        if (fetched.Count == 0)
        {
            result.Errors.Add(string.IsNullOrWhiteSpace(provider.LastError)
                ? $"Provider {provider.Name} returned no exchange rates for base {baseCurrency}."
                : $"Provider {provider.Name} failed: {provider.LastError}");
            return;
        }

        var pairs = fetched
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key)
                && !string.Equals(kv.Key.Trim(), baseCurrency, StringComparison.OrdinalIgnoreCase)
                && kv.Value > 0)
            .Select(kv => (Code: kv.Key.Trim().ToUpperInvariant(), Rate: kv.Value))
            .ToList();

        result.RatesFetched = pairs.Count;
        if (pairs.Count == 0)
        {
            result.Errors.Add($"Provider {provider.Name} returned no usable rates for base {baseCurrency}.");
            return;
        }

        // Target tenants (deliberately conservative): every DISTINCT TenantId already present in
        // exchange_rates, plus the caller's current tenant when there is one (API triggered runs).
        // Background runs have no HTTP context, so ITenantContext.TenantId is Guid.Empty there.
        // Active tenants that never had a rate row are left untouched - the audit trail stays scoped
        // to tenants that actually opted into rate data. Extend with _db.Tenants (Status == "ACTIVE")
        // here if product wants first-time rows for brand new tenants.
        var tenantIds = await ResolveTenantIdsAsync(ct);
        if (tenantIds.Count == 0)
        {
            result.Errors.Add("No target tenant was found for the exchange rate sync.");
            return;
        }

        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        foreach (var tenantId in tenantIds)
        {
            if (ct.IsCancellationRequested)
            {
                result.Errors.Add("The exchange rate sync run was cancelled.");
                break;
            }

            try
            {
                await UpsertTenantRatesAsync(tenantId, baseCurrency, pairs, provider.Name, today, now, result, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One failing tenant must never stop the others.
                _logger.LogError(ex, "Exchange rate sync failed for tenant {TenantId}", tenantId);
                result.Errors.Add($"Tenant {tenantId}: {ex.Message}");
            }
        }

        if (result.Errors.Count == 0 && result.RatesFetched > 0)
        {
            await WriteSyncAuditAsync(result, tenantIds.Count);
        }
    }

    private async Task<List<Guid>> ResolveTenantIdsAsync(CancellationToken ct)
    {
        var tenantIds = await _db.ExchangeRates
            .Where(r => r.TenantId != Guid.Empty)
            .Select(r => r.TenantId)
            .Distinct()
            .ToListAsync(ct);

        var current = _tenant.TenantId;
        if (current != Guid.Empty && !tenantIds.Contains(current)) tenantIds.Add(current);
        return tenantIds;
    }

    private async Task UpsertTenantRatesAsync(
        Guid tenantId,
        string baseCurrency,
        List<(string Code, decimal Rate)> pairs,
        string source,
        DateTime today,
        DateTime now,
        ExchangeRateSyncResultDto result,
        CancellationToken ct)
    {
        var transaction = await TryBeginTransactionAsync(ct);
        var inserted = 0;
        var updated = 0;
        try
        {
            foreach (var (code, rate) in pairs)
            {
                var existing = await _db.ExchangeRates
                    .Where(r => r.TenantId == tenantId
                        && r.FromCurrency == baseCurrency
                        && r.ToCurrency == code
                        && r.EffectiveTo == null)
                    .OrderByDescending(r => r.EffectiveFrom)
                    .FirstOrDefaultAsync(ct);

                if (existing != null)
                {
                    existing.Rate = rate;
                    existing.EffectiveFrom = today;
                    existing.Source = source;
                    existing.UpdatedAt = now;
                    updated++;
                }
                else
                {
                    _db.ExchangeRates.Add(new ExchangeRateEntity
                    {
                        TenantId = tenantId,
                        FromCurrency = baseCurrency,
                        ToCurrency = code,
                        Rate = rate,
                        EffectiveFrom = today,
                        EffectiveTo = null,
                        Source = source
                    });
                    inserted++;
                }
            }

            await _db.SaveChangesAsync(ct);

            if (transaction != null) await transaction.CommitAsync(ct);

            // Only committed work is reported.
            result.Inserted += inserted;
            result.Updated += updated;
        }
        catch
        {
            if (transaction != null)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch (Exception rollbackEx) { _logger.LogWarning(rollbackEx, "Rollback failed for tenant {TenantId}", tenantId); }
            }

            // Discard any half-tracked entities so the next tenant starts clean.
            _db.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }

    private async Task WriteSyncAuditAsync(ExchangeRateSyncResultDto result, int tenantCount)
    {
        try
        {
            await _audit.LogAsync(new AuditEntryDto
            {
                TenantId = _tenant.TenantId == Guid.Empty ? (Guid?)null : _tenant.TenantId,
                ModuleName = "FI",
                ActionType = ActionType.Config,
                EntityName = "ExchangeRate",
                EntityId = result.Provider,
                Details = $"Exchange rate sync via {result.Provider} (base {result.BaseCurrency}) for {tenantCount} tenant(s): {result.RatesFetched} rates fetched, {result.Inserted} inserted, {result.Updated} updated.",
                NewValue = System.Text.Json.JsonSerializer.Serialize(new
                {
                    result.Provider,
                    result.BaseCurrency,
                    result.RatesFetched,
                    result.Inserted,
                    result.Updated,
                    Tenants = tenantCount
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write the exchange rate sync audit entry");
        }
    }

    private async Task<IDbContextTransaction?> TryBeginTransactionAsync(CancellationToken ct)
    {
        try
        {
            return await _db.Database.BeginTransactionAsync(ct);
        }
        catch (Exception ex)
        {
            // InMemory test databases do not support transactions - continue without one.
            _logger.LogInformation(ex, "No database transaction available for the exchange rate upsert; continuing without one.");
            return null;
        }
    }
}
