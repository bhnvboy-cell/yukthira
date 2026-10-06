using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Web.Middleware;

public class SecurityGuardMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] AdminModules = ["Admin", "Audit", "Integration", "Plugins", "TCode", "TCodeGenerator", "Customization"];
    private static readonly HashSet<string> PowerUserPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/BI/Dashboard/Create",
        "/BI/Report/Create",
        "/Workflow/Instances",
        "/Workflow/Designer"
    };

    private static DateTime _allowTraceCheckedAt = DateTime.MinValue;
    private static bool _traceAllowedPages;

    public SecurityGuardMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (IsApiPath(path))
        {
            await _next(context);
            return;
        }

        var route = path.TrimStart('/').TrimEnd('/');
        var topFolder = route.Split('/')[0];
        var authenticated = context.User?.Identity?.IsAuthenticated == true;

        if (topFolder != "Auth" && topFolder != "health" && !authenticated)
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                context.Response.Redirect("/Auth/Login?ReturnUrl=" + Uri.EscapeDataString(path + context.Request.QueryString));
                return;
            }
            context.Response.StatusCode = 401;
            return;
        }

        string? deniedBy = null;
        string? ruleSource = null;
        string? reason = null;

        if (AdminModules.Contains(topFolder, StringComparer.OrdinalIgnoreCase))
        {
            if (context.User?.IsInRole("SUPER_USER") != true && context.User?.IsInRole("ADMIN") != true)
            {
                deniedBy = "/" + route;
                ruleSource = "FolderRule";
                reason = $"The '{topFolder}' area requires SUPER_USER or ADMIN";
            }
        }
        else if (PowerUserPages.Contains("/" + route))
        {
            if (context.User?.IsInRole("SUPER_USER") != true &&
                context.User?.IsInRole("ADMIN") != true &&
                context.User?.IsInRole("POWER_USER") != true)
            {
                deniedBy = "/" + route;
                ruleSource = "PageRule";
                reason = $"The page '{route}' requires POWER_USER or above";
            }
        }

        if (deniedBy is not null)
        {
            await TraceAsync(context, deniedBy, "Deny", ruleSource!, reason!);
            var isGet = HttpMethods.IsGet(context.Request.Method);
            var wantsJson = context.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
                || context.Request.Headers.XRequestedWith == "XMLHttpRequest";
            if (isGet && !wantsJson)
            {
                context.Response.Redirect("/Auth/AccessDenied");
                return;
            }
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                $"{{\"error\":\"Access denied\",\"path\":\"{System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(deniedBy)}\",\"ruleSource\":\"{ruleSource}\",\"reason\":\"{System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(reason)}\"}}");
            return;
        }

        if (authenticated && !IsApiPath(path) && ShouldTraceAllowedPages(context))
        {
            await TraceAsync(context, "/" + route, "Allow", "FolderRule", "Navigation allowed by folder rules");
        }

        await _next(context);
    }

    private static bool IsApiPath(string path) =>
        path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldTraceAllowedPages(HttpContext context)
    {
        if (DateTime.UtcNow < _allowTraceCheckedAt) return _traceAllowedPages;
        try
        {
            var db = context.RequestServices.GetRequiredService<YuktiraDbContext>();
            var value = db.SystemConfigs.AsNoTracking()
                .FirstOrDefault(c => c.Key == "features.authz_trace_allow")?.Value;
            _traceAllowedPages = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            _traceAllowedPages = false;
        }
        _allowTraceCheckedAt = DateTime.UtcNow.AddSeconds(60);
        return _traceAllowedPages;
    }

    private static async Task TraceAsync(HttpContext context, string resource, string decision, string ruleSource, string reason)
    {
        try
        {
            var trace = context.RequestServices.GetRequiredService<IAuthorizationTraceService>();
            var tenant = context.RequestServices.GetService<ITenantContext>();
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value;
            Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;
            await trace.TraceAsync(new AuthorizationTraceEntry
            {
                TenantId = tenant?.TenantId ?? Guid.Empty,
                UserId = userId,
                UserName = context.User.FindFirst(ClaimTypes.Name)?.Value ?? context.User.Identity?.Name ?? "",
                Role = context.User.FindFirst(ClaimTypes.Role)?.Value ?? context.User.FindFirst("role")?.Value ?? "",
                SessionId = context.TraceIdentifier,
                CorrelationId = Guid.NewGuid().ToString("N"),
                ResourceType = "Page",
                Resource = resource,
                Decision = decision,
                RuleSource = ruleSource,
                Reason = reason,
                HttpMethod = context.Request.Method,
                HttpPath = context.Request.Path,
                IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = context.Request.Headers.UserAgent.ToString()
            });
        }
        catch
        {
        }
    }
}
