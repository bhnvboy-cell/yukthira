using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

/// <summary>
/// Tenant exchange rate matrix, sync trigger/status, SOX controlled manual overrides and a
/// conversion simulator. Live route: api/v1/fi/exchange-rates (ApiVersionRouteConvention).
/// </summary>
[ApiController]
[Route("api/fi/exchange-rates")]
[Authorize]
public class ExchangeRateController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;
    private readonly IExchangeRateSyncService _sync;
    private readonly IExchangeRateSyncStatusStore _status;
    private readonly ExchangeRateSyncOptions _options;

    public ExchangeRateController(
        YuktiraDbContext db,
        ITenantContext tenant,
        IAuditService audit,
        IExchangeRateSyncService sync,
        IExchangeRateSyncStatusStore status,
        IOptions<ExchangeRateSyncOptions> options)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
        _sync = sync;
        _status = status;
        _options = options.Value;
    }

    // GET api/v1/fi/exchange-rates - tenant rate matrix + base currency + last sync info
    [HttpGet]
    public async Task<IActionResult> GetMatrix()
    {
        var tenantId = _tenant.TenantId;
        var rows = await _db.ExchangeRates
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.FromCurrency)
            .ThenBy(r => r.ToCurrency)
            .ToListAsync();

        var data = rows.Select(r => new ExchangeRateMatrixRowDto
        {
            From = r.FromCurrency,
            To = r.ToCurrency,
            Rate = r.Rate,
            Inverse = ExchangeRateMath.InverseRate(r.Rate),
            Source = r.Source,
            EffectiveFrom = r.EffectiveFrom,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        return Ok(new
        {
            data,
            tenantId,
            baseCurrency = BaseCurrency,
            lastSync = GetSyncStatusPayload()
        });
    }

    // GET api/v1/fi/exchange-rates/sync-status
    [HttpGet("sync-status")]
    public IActionResult GetSyncStatus()
    {
        return Ok(new { data = GetSyncStatusPayload(), tenantId = _tenant.TenantId });
    }

    // POST api/v1/fi/exchange-rates/sync - same logic as the background job
    [HttpPost("sync")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> TriggerSync()
    {
        if (_status.IsRunning)
        {
            return Problem(
                detail: "An exchange rate sync run is already in progress. Try again shortly.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Sync already running");
        }

        var result = await _sync.SyncAsync(HttpContext.RequestAborted);
        return Ok(new { success = result.Success, data = result, tenantId = _tenant.TenantId });
    }

    // POST api/v1/fi/exchange-rates/override - manual rate change with mandatory SOX comment
    [HttpPost("override")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> OverrideRate([FromBody] ManualRateOverrideRequest request)
    {
        if (request == null)
            return Problem(detail: "A request body is required.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid request");

        if (string.IsNullOrWhiteSpace(request.Comment))
            return Problem(
                detail: "A comment is required for a manual exchange rate override (SOX control).",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Comment required");

        var from = (request.From ?? "").Trim().ToUpperInvariant();
        var to = (request.To ?? "").Trim().ToUpperInvariant();
        if (from.Length == 0 || to.Length == 0)
            return Problem(detail: "Both From and To currency codes are required.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid currency pair");
        if (from == to)
            return Problem(detail: "From and To currency cannot be the same.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid currency pair");
        if (request.Rate <= 0)
            return Problem(detail: "Exchange rate must be greater than zero.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid rate");

        var tenantId = _tenant.TenantId;
        if (tenantId == Guid.Empty)
            return Problem(detail: "The current tenant could not be resolved.", statusCode: StatusCodes.Status400BadRequest, title: "Tenant required");

        var comment = request.Comment.Trim();
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;
        decimal? oldRate = null;

        var transaction = await TryBeginTransactionAsync(HttpContext.RequestAborted);
        try
        {
            var existing = await _db.ExchangeRates
                .Where(r => r.TenantId == tenantId
                    && r.FromCurrency == from
                    && r.ToCurrency == to
                    && r.EffectiveTo == null)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync(HttpContext.RequestAborted);

            if (existing != null)
            {
                oldRate = existing.Rate;
                existing.Rate = request.Rate;
                existing.EffectiveFrom = today;
                existing.Source = "Manual";
                existing.UpdatedAt = now;
            }
            else
            {
                _db.ExchangeRates.Add(new ExchangeRateEntity
                {
                    TenantId = tenantId,
                    FromCurrency = from,
                    ToCurrency = to,
                    Rate = request.Rate,
                    EffectiveFrom = today,
                    EffectiveTo = null,
                    Source = "Manual"
                });
            }

            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            if (transaction != null) await transaction.CommitAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch { /* best effort */ }
            }
            _db.ChangeTracker.Clear();
            return Problem(
                detail: $"The manual rate override could not be saved: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Override failed");
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = tenantId,
            ModuleName = "FI",
            ActionType = ActionType.Update,
            EntityName = "ExchangeRate",
            EntityId = $"{from}/{to}",
            OldValue = oldRate?.ToString(CultureInfo.InvariantCulture),
            NewValue = request.Rate.ToString(CultureInfo.InvariantCulture),
            Details = comment
        });

        return Ok(new
        {
            success = true,
            tenantId,
            from,
            to,
            rate = request.Rate,
            source = "Manual",
            effectiveFrom = today
        });
    }

    // POST api/v1/fi/exchange-rates/convert - exact pair, inverse, or cross through a common base
    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] ExchangeRateConvertRequest request)
    {
        if (request == null)
            return Problem(detail: "A request body is required.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid request");

        var from = (request.From ?? "").Trim().ToUpperInvariant();
        var to = (request.To ?? "").Trim().ToUpperInvariant();
        if (from.Length == 0 || to.Length == 0)
            return Problem(detail: "Both From and To currency codes are required.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid currency pair");
        if (request.Amount < 0)
            return Problem(detail: "Amount cannot be negative.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid amount");

        var tenantId = _tenant.TenantId;
        var today = DateTime.UtcNow.Date;

        var rows = await _db.ExchangeRates
            .Where(r => r.TenantId == tenantId
                && r.EffectiveFrom <= today
                && (r.EffectiveTo == null || r.EffectiveTo >= today))
            .ToListAsync();

        var lookup = new Dictionary<(string From, string To), decimal>();
        foreach (var row in rows.OrderBy(r => r.EffectiveFrom))
        {
            if (row.Rate <= 0) continue;
            if (string.IsNullOrWhiteSpace(row.FromCurrency) || string.IsNullOrWhiteSpace(row.ToCurrency)) continue;
            lookup[(row.FromCurrency.Trim().ToUpperInvariant(), row.ToCurrency.Trim().ToUpperInvariant())] = row.Rate;
        }

        if (!ExchangeRateMath.TryResolveRate(lookup, from, to, out var rate))
        {
            return Problem(
                detail: $"No exchange rate path is available to convert {from} to {to} for this tenant. Run a sync or add a manual rate first.",
                statusCode: StatusCodes.Status404NotFound,
                title: "Exchange rate not found");
        }

        rate = Math.Round(rate, 8, MidpointRounding.AwayFromZero);
        var converted = request.Amount * rate;

        var asOf = rows
            .Where(r => r.FromCurrency.Equals(from, StringComparison.OrdinalIgnoreCase)
                || r.ToCurrency.Equals(from, StringComparison.OrdinalIgnoreCase)
                || r.FromCurrency.Equals(to, StringComparison.OrdinalIgnoreCase)
                || r.ToCurrency.Equals(to, StringComparison.OrdinalIgnoreCase))
            .Select(r => (DateTime?)r.EffectiveFrom)
            .Max() ?? DateTime.UtcNow;

        return Ok(new
        {
            success = true,
            data = new ExchangeRateConvertResultDto
            {
                Amount = request.Amount,
                From = from,
                To = to,
                Rate = rate,
                Converted = converted,
                AsOf = asOf
            },
            tenantId
        });
    }

    private string BaseCurrency =>
        string.IsNullOrWhiteSpace(_options.BaseCurrency) ? "EUR" : _options.BaseCurrency.Trim().ToUpperInvariant();

    private object GetSyncStatusPayload() => new
    {
        isRunning = _status.IsRunning,
        lastRunAt = _status.LastRunAt,
        lastResult = _status.LastResult
    };

    private async Task<IDbContextTransaction?> TryBeginTransactionAsync(CancellationToken ct)
    {
        try
        {
            return await _db.Database.BeginTransactionAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    private Guid GetUserId() =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
}
