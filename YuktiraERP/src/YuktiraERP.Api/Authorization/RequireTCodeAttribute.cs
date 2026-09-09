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
        var transactionCodeService = context.HttpContext.RequestServices.GetRequiredService<ITransactionCodeService>();

        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? context.HttpContext.User.FindFirst("sub")?.Value;
        var roleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? context.HttpContext.User.FindFirst("role")?.Value;

        Guid? userId = null;
        if (Guid.TryParse(userIdClaim, out var parsed)) userId = parsed;

        var hasAccess = await transactionCodeService.ValidateAccessAsync(TCode, userId, roleClaim);

        if (!hasAccess)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
