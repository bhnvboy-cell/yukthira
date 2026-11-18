using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Mes.Infrastructure.BackgroundServices;
using YuktiraERP.Mes.Infrastructure.Data;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Infrastructure;

public static class MesInfrastructureRegistration
{
    public static IServiceCollection AddYuktiraMesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("YuktiraDb");
        if (!string.IsNullOrEmpty(connStr))
        {
            services.AddDbContext<MesDbContext>((sp, options) =>
            {
                var interceptor = sp.GetService<TenantSaveChangesInterceptor>();
                options.UseNpgsql(connStr, npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Mes", MesDbContext.SchemaName));
                if (interceptor != null)
                    options.AddInterceptors(interceptor);
            });
        }
        else
        {
            services.AddDbContext<MesDbContext>((sp, options) =>
            {
                var interceptor = sp.GetService<TenantSaveChangesInterceptor>();
                options.UseInMemoryDatabase("YuktiraERP_Mes");
                if (interceptor != null)
                    options.AddInterceptors(interceptor);
            });
        }

        services.AddScoped<MesResourceService>();
        services.AddScoped<MesSchedulingService>();
        services.AddScoped<MesDispatchService>();
        services.AddScoped<MesDocumentService>();
        services.AddScoped<MesDataCollectionService>();
        services.AddScoped<MesLaborService>();
        services.AddScoped<MesQualityService>();
        services.AddScoped<MesProcessService>();
        services.AddScoped<MesMaintenanceService>();
        services.AddScoped<MesGenealogyService>();
        services.AddScoped<MesOeeService>();
        services.AddScoped<MesErpPostingBridge>();
        services.AddHostedService<MesSchemaInitializerHostedService>();
        services.AddHostedService<MesOutboxPublisherService>();

        return services;
    }
}
