using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireTCodeAttribute : Attribute, IAsyncActionFilter
{
    public string TCode { get; }

    public RequireTCodeAttribute(string tCode)
    {
        TCode = tCode;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var transactionCodeService = http.RequestServices.GetRequiredService<ITransactionCodeService>();
        var traceService = http.RequestServices.GetRequiredService<IAuthorizationTraceService>();

        var userIdClaim = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? http.User.FindFirst("sub")?.Value;
        var roleClaim = http.User.FindFirst(ClaimTypes.Role)?.Value ?? http.User.FindFirst("role")?.Value;
        var userName = http.User.FindFirst(ClaimTypes.Name)?.Value ?? http.User.Identity?.Name ?? "";

        Guid? userId = null;
        if (Guid.TryParse(userIdClaim, out var parsed)) userId = parsed;

        var result = await transactionCodeService.CheckAccessDetailedAsync(TCode, userId, roleClaim);

        await TraceAsync(traceService, http, userId, userName, roleClaim, result.Allowed, result.RuleSource, result.Reason, TCode);

        if (!result.Allowed)
        {
            context.Result = new ObjectResult(new
            {
                error = "Access denied",
                tcode = TCode,
                ruleSource = result.RuleSource,
                reason = result.Reason
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        await next();
    }

    internal static async Task TraceAsync(
        IAuthorizationTraceService traceService,
        HttpContext http,
        Guid? userId,
        string userName,
        string? role,
        bool allowed,
        string ruleSource,
        string reason,
        string resource = "")
    {
        try
        {
            var tenantId = http.RequestServices.GetService<ITenantContext>()?.TenantId ?? Guid.Empty;
            await traceService.TraceAsync(new AuthorizationTraceEntry
            {
                TenantId = tenantId,
                UserId = userId,
                UserName = userName,
                Role = role ?? "",
                SessionId = http.TraceIdentifier,
                CorrelationId = Guid.NewGuid().ToString("N"),
                ResourceType = "TCode",
                Resource = resource,
                Decision = allowed ? "Allow" : "Deny",
                RuleSource = ruleSource,
                Reason = reason,
                HttpMethod = http.Request.Method,
                HttpPath = http.Request.Path,
                IpAddress = http.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = http.Request.Headers.UserAgent.ToString()
            });
        }
        catch
        {
        }
    }
}
