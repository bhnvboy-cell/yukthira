using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Infrastructure.Caching;

namespace YuktiraERP.Infrastructure.MultiTenant;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedCacheService? _cache;

    public TenantMiddleware(RequestDelegate next, IDistributedCacheService? cache = null)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = await ResolveTenantAsync(context);
        if (tenantId.HasValue)
        {
            context.Items["TenantId"] = tenantId.Value;
        }
        await _next(context);
    }

    private async Task<Guid?> ResolveTenantAsync(HttpContext context)
    {
        // Check cache first
        if (_cache != null)
        {
            var cacheKey = $"tenant:resolve:{context.Request.Host.Host}:{context.Request.Path}";
            var cachedStr = await _cache.GetStringAsync(cacheKey);
            if (cachedStr != null && Guid.TryParse(cachedStr, out var cachedTenantId))
            {
                return cachedTenantId;
            }
        }

        var tenantService = context.RequestServices.GetService<ITenantResolver>();
        var tenantId = tenantService?.ResolveTenant(context);

        // Cache the result
        if (_cache != null && tenantId.HasValue)
        {
            var cacheKey = $"tenant:resolve:{context.Request.Host.Host}:{context.Request.Path}";
            await _cache.SetStringAsync(cacheKey, tenantId.Value.ToString(), TimeSpan.FromMinutes(5));
        }

        return tenantId;
    }
}

public interface ITenantResolver
{
    Guid? ResolveTenant(HttpContext context);
}

public class TenantResolver : ITenantResolver
{
    public Guid? ResolveTenant(HttpContext context)
    {
        // NOTE: X-Tenant-Id is deliberately NOT trusted — it is client-controlled and
        // could be used to switch tenants. Tenant identity must come from the authenticated
        // user's claims (see TenantContext), never from request headers.

        var host = context.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            var parts = host.Split('.');
            if (parts.Length >= 2)
            {
                var subdomain = parts[0].ToLower();
                var tenantCode = subdomain.Replace("-", "").Replace("_", "");
                if (!string.IsNullOrEmpty(tenantCode) && tenantCode != "www" && tenantCode != "app" && tenantCode != "api")
                {
                    return ParseTenantCode(tenantCode);
                }
            }
        }

        if (context.Request.Path.HasValue)
        {
            var segments = context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0 && segments[0].Length <= 10)
            {
                return ParseTenantCode(segments[0]);
            }
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst("TenantId");
            if (claim != null && Guid.TryParse(claim.Value, out var tid))
                return tid;
        }

        return null;
    }

    private static Guid? ParseTenantCode(string code)
    {
        var knownTenants = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            { "demo", Guid.Parse("00000000-0000-0000-0000-000000000001") },
            { "acme", Guid.Parse("00000000-0000-0000-0000-000000000002") },
            { "globex", Guid.Parse("00000000-0000-0000-0000-000000000003") }
        };
        return knownTenants.TryGetValue(code, out var tid) ? tid : null;
    }
}
