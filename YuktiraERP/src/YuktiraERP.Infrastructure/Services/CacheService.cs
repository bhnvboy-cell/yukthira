using Microsoft.Extensions.Caching.Memory;

namespace YuktiraERP.Infrastructure.Services;

public class CacheService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DefaultTTL = TimeSpan.FromMinutes(5);

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key) where T : class
        => _cache.TryGetValue(key, out T? value) ? value : null;

    public void Set<T>(string key, T value, TimeSpan? ttl = null) where T : class
        => _cache.Set(key, value, ttl ?? DefaultTTL);

    public void Remove(string key) => _cache.Remove(key);

    public bool Exists(string key) => _cache.TryGetValue(key, out _);

    public string BuildKey(string prefix, string identifier)
        => $"yuktira:{prefix}:{identifier}";
}
