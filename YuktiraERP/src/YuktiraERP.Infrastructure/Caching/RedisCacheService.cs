using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace YuktiraERP.Infrastructure.Caching;

public class RedisCacheOptions
{
    public string Connection { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "yuktira_";
    public int DefaultExpirationMinutes { get; set; } = 30;
}

public class RedisCacheService : IDistributedCacheService, IDisposable
{
    private readonly ILogger<RedisCacheService> _logger;
    private readonly RedisCacheOptions _options;
    private readonly Lazy<ConnectionMultiplexer?> _lazyConnection;
    private ConnectionMultiplexer? _connection;
    private bool _disposed;

    public bool IsAvailable => _connection?.IsConnected == true;

    public RedisCacheService(IOptions<RedisCacheOptions> options, ILogger<RedisCacheService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _lazyConnection = new Lazy<ConnectionMultiplexer?>(() =>
        {
            try
            {
                var opts = ConfigurationOptions.Parse(_options.Connection);
                opts.AbortOnConnectFail = false;
                opts.ConnectTimeout = 5000;
                opts.SyncTimeout = 3000;
                return ConnectionMultiplexer.Connect(opts);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to Redis at {Connection}", _options.Connection);
                return null;
            }
        });
        _connection = _lazyConnection.Value;
    }

    private IDatabase? GetDb() => _connection?.GetDatabase();

    private string PrefixKey(string key) => $"{_options.InstanceName}{key}";

    public async Task<string?> GetStringAsync(string key)
    {
        if (!IsAvailable) return null;
        try
        {
            var db = GetDb();
            var value = await db!.StringGetAsync(PrefixKey(key));
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET STRING failed for key {Key}", key);
            return null;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        if (!IsAvailable) return;
        try
        {
            var db = GetDb();
            var ttl = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
            await db!.StringSetAsync(PrefixKey(key), value, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET STRING failed for key {Key}", key);
        }
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var json = await GetStringAsync(key);
        if (json == null) return null;
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await SetStringAsync(key, json, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        if (!IsAvailable) return;
        try
        {
            var db = GetDb();
            await db!.KeyDeleteAsync(PrefixKey(key));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for key {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        if (!IsAvailable) return;
        try
        {
            var db = GetDb();
            var server = _connection!.GetServer(_connection.GetEndPoints().First());
            var keys = server.Keys(pattern: PrefixKey(pattern));
            foreach (var key in keys)
            {
                await db!.KeyDeleteAsync(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE BY PATTERN failed for pattern {Pattern}", pattern);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;

        var value = await factory();
        await SetAsync(key, value, expiry);
        return value;
    }

    public async Task FlushTenantCache(Guid tenantId)
    {
        await RemoveByPatternAsync($"tenant:{tenantId}:*");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
