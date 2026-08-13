using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class CurrencyService : ICurrencyService
{
    private readonly YuktiraDbContext _db;

    public CurrencyService(YuktiraDbContext db) { _db = db; }

    public async Task<List<CurrencyDto>> GetCurrenciesAsync(Guid tenantId)
    {
        return await _db.Currencies
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.IsBase)
            .ThenBy(c => c.Code)
            .Select(c => new CurrencyDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Symbol = c.Symbol,
                IsBase = c.IsBase,
                DecimalPlaces = c.DecimalPlaces,
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public async Task<CurrencyDto> CreateCurrencyAsync(Guid tenantId, CurrencyDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new InvalidOperationException("Currency code is required");

        var code = request.Code.ToUpperInvariant();
        var exists = await _db.Currencies.AnyAsync(c => c.TenantId == tenantId && c.Code == code);
        if (exists)
            throw new InvalidOperationException($"Currency {code} already exists");

        if (request.IsBase)
        {
            var anyBase = await _db.Currencies.AnyAsync(c => c.TenantId == tenantId && c.IsBase);
            if (anyBase)
                throw new InvalidOperationException("A base currency already exists. Set the new currency as a secondary currency or deactivate the current base.");
        }

        var entity = new CurrencyEntity
        {
            TenantId = tenantId,
            Code = code,
            Name = request.Name,
            Symbol = request.Symbol,
            IsBase = request.IsBase,
            DecimalPlaces = request.DecimalPlaces,
            IsActive = request.IsActive
        };
        _db.Currencies.Add(entity);
        await _db.SaveChangesAsync();

        return new CurrencyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Symbol = entity.Symbol,
            IsBase = entity.IsBase,
            DecimalPlaces = entity.DecimalPlaces,
            IsActive = entity.IsActive
        };
    }

    public async Task<CurrencyDto?> UpdateCurrencyAsync(Guid tenantId, Guid id, CurrencyDto request)
    {
        var entity = await _db.Currencies.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id);
        if (entity == null) return null;

        if (request.IsBase)
        {
            await _db.Currencies
                .Where(c => c.TenantId == tenantId && c.IsBase && c.Id != id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsBase, false));
        }

        entity.Name = request.Name;
        entity.Symbol = request.Symbol;
        entity.IsBase = request.IsBase;
        entity.DecimalPlaces = request.DecimalPlaces;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new CurrencyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Symbol = entity.Symbol,
            IsBase = entity.IsBase,
            DecimalPlaces = entity.DecimalPlaces,
            IsActive = entity.IsActive
        };
    }

    public async Task DeleteCurrencyAsync(Guid tenantId, Guid id)
    {
        var entity = await _db.Currencies.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id);
        if (entity == null) return;
        if (entity.IsBase)
            throw new InvalidOperationException("Cannot delete the base currency");

        var inUse = await _db.ExchangeRates.AnyAsync(r => r.TenantId == tenantId
            && (r.FromCurrency == entity.Code || r.ToCurrency == entity.Code));
        if (inUse)
            throw new InvalidOperationException($"Currency {entity.Code} is used by exchange rates and cannot be deleted");

        _db.Currencies.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<CurrencyDto?> GetBaseCurrencyAsync(Guid tenantId)
    {
        return await _db.Currencies
            .Where(c => c.TenantId == tenantId && c.IsBase)
            .Select(c => new CurrencyDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Symbol = c.Symbol,
                IsBase = c.IsBase,
                DecimalPlaces = c.DecimalPlaces,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<ExchangeRateDto>> GetExchangeRatesAsync(Guid tenantId, string? from = null, string? to = null)
    {
        var query = _db.ExchangeRates.Where(r => r.TenantId == tenantId);
        if (!string.IsNullOrEmpty(from)) query = query.Where(r => r.FromCurrency == from);
        if (!string.IsNullOrEmpty(to)) query = query.Where(r => r.ToCurrency == to);

        return await query
            .OrderByDescending(r => r.EffectiveFrom)
            .Select(r => new ExchangeRateDto
            {
                Id = r.Id,
                FromCurrency = r.FromCurrency,
                ToCurrency = r.ToCurrency,
                Rate = r.Rate,
                EffectiveFrom = r.EffectiveFrom,
                EffectiveTo = r.EffectiveTo,
                Source = r.Source
            })
            .ToListAsync();
    }

    public async Task<ExchangeRateDto> SetExchangeRateAsync(Guid tenantId, ExchangeRateDto request)
    {
        var from = request.FromCurrency.ToUpperInvariant();
        var to = request.ToCurrency.ToUpperInvariant();
        if (from == to)
            throw new InvalidOperationException("From and To currency cannot be the same");
        if (request.Rate <= 0)
            throw new InvalidOperationException("Exchange rate must be positive");

        await EnsureCurrencyExistsAsync(tenantId, from);
        await EnsureCurrencyExistsAsync(tenantId, to);

        var existing = await _db.ExchangeRates
            .FirstOrDefaultAsync(r => r.TenantId == tenantId
                && r.FromCurrency == from && r.ToCurrency == to
                && r.EffectiveTo == null);
        if (existing != null)
        {
            existing.Rate = request.Rate;
            existing.Source = request.Source;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ExchangeRates.Add(new ExchangeRateEntity
            {
                TenantId = tenantId,
                FromCurrency = from,
                ToCurrency = to,
                Rate = request.Rate,
                EffectiveFrom = request.EffectiveFrom == default ? DateTime.Today : request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo,
                Source = request.Source
            });
        }

        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<CurrencyConversionResult> ConvertAsync(Guid tenantId, CurrencyConversionRequest request)
    {
        if (request.Amount < 0)
            throw new InvalidOperationException("Amount cannot be negative");

        var from = request.FromCurrency.ToUpperInvariant();
        var to = request.ToCurrency.ToUpperInvariant();
        if (from == to)
        {
            return new CurrencyConversionResult
            {
                Amount = request.Amount,
                FromCurrency = from,
                ToCurrency = to,
                Rate = 1,
                ConvertedAmount = request.Amount
            };
        }

        var rate = await GetRateAsync(tenantId, from, to, request.AsOf);
        return new CurrencyConversionResult
        {
            Amount = request.Amount,
            FromCurrency = from,
            ToCurrency = to,
            Rate = rate,
            ConvertedAmount = request.Amount * rate
        };
    }

    public async Task<decimal> GetRateAsync(Guid tenantId, string from, string to, DateTime? asOf = null)
    {
        var asOfDate = asOf ?? DateTime.Today;

        // Direct rate
        var direct = await _db.ExchangeRates
            .Where(r => r.TenantId == tenantId && r.FromCurrency == from && r.ToCurrency == to
                && r.EffectiveFrom <= asOfDate && (r.EffectiveTo == null || r.EffectiveTo >= asOfDate))
            .OrderByDescending(r => r.EffectiveFrom)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefaultAsync();
        if (direct.HasValue) return direct.Value;

        // Inverse rate
        var inverse = await _db.ExchangeRates
            .Where(r => r.TenantId == tenantId && r.FromCurrency == to && r.ToCurrency == from
                && r.EffectiveFrom <= asOfDate && (r.EffectiveTo == null || r.EffectiveTo >= asOfDate))
            .OrderByDescending(r => r.EffectiveFrom)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefaultAsync();
        if (inverse.HasValue && inverse.Value != 0) return 1 / inverse.Value;

        // Try via base currency (USD or the tenant base)
        var baseCode = (await GetBaseCurrencyAsync(tenantId))?.Code ?? "USD";
        if (baseCode != from && baseCode != to)
        {
            var viaBase = await GetRateAsync(tenantId, from, baseCode, asOfDate);
            var baseToTarget = await GetRateAsync(tenantId, baseCode, to, asOfDate);
            return viaBase * baseToTarget;
        }

        throw new InvalidOperationException($"No exchange rate found from {from} to {to} as of {asOfDate:yyyy-MM-dd}. Set one first.");
    }

    public async Task<List<CurrencyConversionResult>> RevaluateAsync(Guid tenantId, CurrencyRevaluationRequest request)
    {
        var to = request.ToCurrency.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(to))
            throw new InvalidOperationException("Target currency is required");
        await EnsureCurrencyExistsAsync(tenantId, to);

        var currencies = await GetCurrenciesAsync(tenantId);
        var results = new List<CurrencyConversionResult>();

        foreach (var currency in currencies.Where(c => c.Code != to && c.IsActive))
        {
            var rate = await GetRateAsync(tenantId, currency.Code, to, request.AsOf);
            results.Add(new CurrencyConversionResult
            {
                Amount = 0,
                FromCurrency = currency.Code,
                ToCurrency = to,
                Rate = rate,
                ConvertedAmount = rate
            });
        }
        return results;
    }

    private async Task EnsureCurrencyExistsAsync(Guid tenantId, string code)
    {
        if (!await _db.Currencies.AnyAsync(c => c.TenantId == tenantId && c.Code == code))
            throw new InvalidOperationException($"Currency {code} is not configured for this tenant. Create it first.");
    }
}