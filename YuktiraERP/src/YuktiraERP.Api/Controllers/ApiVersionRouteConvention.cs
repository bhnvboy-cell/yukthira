using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace YuktiraERP.Api.Controllers;

/// <summary>
/// Prefixes every controller route with the current API version,
/// turning "api/foo" into "api/v1/foo".
/// </summary>
public class ApiVersionRouteConvention : IApplicationModelConvention
{
    public const string Version = "v1";

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors)
            {
                var route = selector.AttributeRouteModel;
                if (route?.Template is null) continue;

                var template = route.Template;
                if (template.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                {
                    selector.AttributeRouteModel = new AttributeRouteModel
                    {
                        Template = "api/" + Version + "/" + template.Substring(4)
                    };
                }
                else if (template.Equals("api", StringComparison.OrdinalIgnoreCase))
                {
                    selector.AttributeRouteModel = new AttributeRouteModel
                    {
                        Template = "api/" + Version
                    };
                }
            }
        }
    }
}
