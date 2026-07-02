using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace YuktiraERP.Infrastructure.MultiTenant;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = ResolveTenant(context);
        if (tenantId.HasValue)
        {
            context.Items["TenantId"] = tenantId.Value;
        }
        await _next(context);
    }

    private static Guid? ResolveTenant(HttpContext context)
    {
        var tenantService = context.RequestServices.GetService<ITenantResolver>();
        return tenantService?.ResolveTenant(context);
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
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
        {
            if (Guid.TryParse(headerTenantId, out var tid))
                return tid;
        }

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
