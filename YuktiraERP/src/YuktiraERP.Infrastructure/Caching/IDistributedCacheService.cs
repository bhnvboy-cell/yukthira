namespace YuktiraERP.Infrastructure.Caching;

public interface IDistributedCacheService
{
    Task<string?> GetStringAsync(string key);
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class;
    Task FlushTenantCache(Guid tenantId);
    bool IsAvailable { get; }
}
