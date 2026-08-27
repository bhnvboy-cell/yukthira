using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace YuktiraERP.Infrastructure.Caching;

public class MemoryCacheFallbackService : IDistributedCacheService
{
    private readonly ILogger<MemoryCacheFallbackService> _logger;
    private readonly ConcurrentDictionary<string, (string Json, DateTime ExpiresAt)> _cache = new();
    private readonly TimeSpan _defaultTtl;

    public bool IsAvailable => true;

    public MemoryCacheFallbackService(ILogger<MemoryCacheFallbackService> logger)
    {
        _logger = logger;
        _defaultTtl = TimeSpan.FromMinutes(30);
    }

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache.Where(kvp => kvp.Value.ExpiresAt <= now).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public Task<string?> GetStringAsync(string key)
    {
        CleanupExpired();
        if (_cache.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            return Task.FromResult<string?>(entry.Json);
        }
        return Task.FromResult<string?>(null);
    }

    public Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        var ttl = expiry ?? _defaultTtl;
        _cache[key] = (value, DateTime.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var json = await GetStringAsync(key);
        if (json == null) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await SetStringAsync(key, json, expiry);
    }

    public Task RemoveAsync(string key)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        var regex = new System.Text.RegularExpressions.Regex(
            "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var keysToRemove = _cache.Keys.Where(k => regex.IsMatch(k)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;

        var value = await factory();
        await SetAsync(key, value, expiry);
        return value;
    }

    public Task FlushTenantCache(Guid tenantId)
    {
        return RemoveByPatternAsync($"tenant:{tenantId}:*");
    }
}
