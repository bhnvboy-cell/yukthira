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
        var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();

        var userId = context.HttpContext.User.Identity?.Name ?? "anonymous";
        var hasAccess = await transactionCodeService.ValidateAccessAsync(TCode, null, null);

        if (!hasAccess)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
