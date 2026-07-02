using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.ExportEngine;

public static class ExportEngineRegistration
{
    public static IServiceCollection AddYuktiraExportEngine(this IServiceCollection services)
    {
        services.AddScoped<IExportService, ExportService>();
        return services;
    }
}
