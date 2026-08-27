using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Caching;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class NumberRangeService : INumberRangeService
{
    private readonly YuktiraDbContext _db;
    private readonly IDistributedCacheService? _cache;
    private static readonly Dictionary<string, long> _localCache = new();
    private static readonly object _lock = new();

    public NumberRangeService(YuktiraDbContext db, IDistributedCacheService? cache = null)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<string> GetNextNumberAsync(Guid tenantId, string module, string prefix, int? year = null)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var key = $"{tenantId}:{module}:{prefix}:{y}";

        // Check distributed cache first, then local cache, then DB
        long? next = null;

        if (_cache != null && _cache.IsAvailable)
        {
            var cachedStr = await _cache.GetStringAsync($"numrange:{key}");
            if (long.TryParse(cachedStr, out var cached))
            {
                next = cached;
                // Sync to local cache
                lock (_lock) { _localCache[key] = cached + 1; }
            }
        }

        if (!next.HasValue)
        {
            lock (_lock)
            {
                if (_localCache.TryGetValue(key, out var cached))
                {
                    next = cached;
                    _localCache[key] = cached + 1;
                }
            }
        }

        if (next.HasValue)
        {
            // Sync distributed cache
            if (_cache != null && _cache.IsAvailable)
                await _cache.SetStringAsync($"numrange:{key}", (next.Value + 1).ToString());
            return $"{prefix}{y}{next.Value:D6}";
        }

        var def = await _db.NumberRangeDefinitions
            .FirstOrDefaultAsync(n => n.TenantId == tenantId && n.Module == module && n.Prefix == prefix);
        if (def == null)
        {
            def = new NumberRangeDefinitionEntity
            {
                TenantId = tenantId,
                Module = module,
                Code = prefix,
                Name = $"{module} {prefix}",
                Prefix = prefix,
                NextNumber = 1
            };
            _db.NumberRangeDefinitions.Add(def);
            await _db.SaveChangesAsync();
        }

        long assigned;
        lock (_lock)
        {
            assigned = def.NextNumber;
            def.NextNumber++;
            _localCache[key] = assigned + 1;
        }
        await _db.SaveChangesAsync();

        // Sync distributed cache
        if (_cache != null && _cache.IsAvailable)
            await _cache.SetStringAsync($"numrange:{key}", (assigned + 1).ToString());

        return $"{prefix}{y}{assigned:D6}";
    }

    public async Task<long> GetCurrentNumberAsync(Guid tenantId, string module, string prefix)
    {
        var key = $"{tenantId}:{module}:{prefix}:{DateTime.UtcNow.Year}";
        lock (_lock)
        {
            if (_localCache.TryGetValue(key, out var cached))
                return cached - 1;
        }
        var def = await _db.NumberRangeDefinitions
            .FirstOrDefaultAsync(n => n.TenantId == tenantId && n.Module == module && n.Prefix == prefix);
        return def?.NextNumber - 1 ?? 0;
    }

    public async Task ResetNumberAsync(Guid tenantId, string module, string prefix, long nextNumber)
    {
        var key = $"{tenantId}:{module}:{prefix}:{DateTime.UtcNow.Year}";
        var def = await _db.NumberRangeDefinitions
            .FirstOrDefaultAsync(n => n.TenantId == tenantId && n.Module == module && n.Prefix == prefix);
        if (def != null)
        {
            def.NextNumber = nextNumber;
            await _db.SaveChangesAsync();
        }
        lock (_lock)
            _localCache[key] = nextNumber;

        if (_cache != null && _cache.IsAvailable)
        {
            await _cache.SetStringAsync($"numrange:{key}", nextNumber.ToString());
        }
    }
}
