using Microsoft.AspNetCore.Mvc.Filters;

namespace YuktiraERP.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Items.ContainsKey("TenantId"))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { error = "Tenant not resolved. Provide X-Tenant-Id header or use valid subdomain." });
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSuperUserAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.FindFirst("IsSuperUser")?.Value != "true")
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
        }
    }
}
