using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class NumberRangeService : INumberRangeService
{
    private readonly YuktiraDbContext _db;
    private static readonly Dictionary<string, long> _cache = new();
    private static readonly object _lock = new();

    public NumberRangeService(YuktiraDbContext db) => _db = db;

    public async Task<string> GetNextNumberAsync(Guid tenantId, string module, string prefix, int? year = null)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var key = $"{tenantId}:{module}:{prefix}:{y}";

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                _cache[key] = cached + 1;
                return Task.FromResult($"{prefix}{y}{cached:D6}").Result;
            }
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

        long next;
        lock (_lock)
        {
            next = def.NextNumber;
            def.NextNumber++;
            _cache[key] = next + 1;
        }
        await _db.SaveChangesAsync();

        return $"{prefix}{y}{next:D6}";
    }

    public async Task<long> GetCurrentNumberAsync(Guid tenantId, string module, string prefix)
    {
        var key = $"{tenantId}:{module}:{prefix}:{DateTime.UtcNow.Year}";
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
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
            _cache[key] = nextNumber;
    }
}
