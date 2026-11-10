using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace YuktiraERP.Api.Controllers;

/// <summary>
/// Dual-routes API controllers to both the current API version ("api/v1/foo")
/// and the direct route ("api/foo") for backwards compatibility with integration scripts.
/// </summary>
public class ApiVersionRouteConvention : IApplicationModelConvention
{
    public const string Version = "v1";

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var selectorsToAdd = new List<SelectorModel>();
            foreach (var selector in controller.Selectors)
            {
                var route = selector.AttributeRouteModel;
                if (route?.Template is null) continue;

                var template = route.Template;
                if (template.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                {
                    selectorsToAdd.Add(new SelectorModel(selector)
                    {
                        AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = "api/" + Version + "/" + template.Substring(4)
                        }
                    });
                }
                else if (template.Equals("api", StringComparison.OrdinalIgnoreCase))
                {
                    selectorsToAdd.Add(new SelectorModel(selector)
                    {
                        AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = "api/" + Version
                        }
                    });
                }
            }

            foreach (var newSelector in selectorsToAdd)
            {
                controller.Selectors.Add(newSelector);
            }
        }
    }
}
