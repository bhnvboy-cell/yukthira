using System.Diagnostics;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        await _next(context);

        sw.Stop();
        if (method != "GET" && context.User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                await auditService.LogAsync(new AuditEntryDto
                {
                    ActionType = ActionType.ApiCall,
                    ModuleName = path.ToString().Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "API",
                    EntityName = path,
                    Details = $"{method} {path} returned {context.Response.StatusCode} in {sw.ElapsedMilliseconds}ms",
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"]
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit logging failed");
            }
        }
    }
}
