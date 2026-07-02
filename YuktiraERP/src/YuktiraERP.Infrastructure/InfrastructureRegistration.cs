using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.MultiTenant;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Infrastructure.Services.Connectors;
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddYuktiraInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("YuktiraDb");
        if (!string.IsNullOrEmpty(connStr))
        {
            services.AddDbContext<YuktiraDbContext>(options => options.UseNpgsql(connStr));
        }
        else
        {
            services.AddDbContext<YuktiraDbContext>(options => options.UseInMemoryDatabase("YuktiraERP"));
        }

        services.AddMemoryCache();
        services.AddHttpClient();

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INumberRangeService, NumberRangeService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPluginService, PluginService>();
        services.AddSingleton<PluginLoader>();
        services.AddScoped<ICustomizationService, CustomizationService>();
        services.AddScoped<IIntegrationHub, IntegrationHubService>();
        services.AddScoped<IIntegrationQueueService, IntegrationQueueService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IDataSyncService, DataSyncService>();
        services.AddSingleton<ConnectorRegistry>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var reg = new ConnectorRegistry();
            reg.Register(new SapS4HanaConnector(http));
            reg.Register(new SapHanaConnector());
            reg.Register(new OracleErpConnector(http));
            reg.Register(new MesConnector(http));
            reg.Register(new LimsConnector(http));
            return reg;
        });
        services.AddScoped<IEdiService, EdiService>();
        services.AddScoped<IKpiService, KpiService>();
        services.AddScoped<ILiveUpdateService, LiveUpdateService>();
        services.AddScoped<IMrpService, MrpService>();
        services.AddScoped<ICapacityPlanningService, CapacityPlanningService>();
        services.AddScoped<IPredictabilityService, PredictabilityService>();
        services.AddScoped<IAccountingService, AccountingService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<ITransactionCodeService, TransactionCodeService>();
        services.AddScoped<IWorkflowEngine, WorkflowService>();
        services.AddScoped<ISuperUserService, SuperUserService>();
        services.AddScoped<IThemeService, ThemeService>();
        services.AddScoped<ITCodeGeneratorService, TCodeGeneratorService>();
        services.AddScoped<ITCodeCustomizationService, TCodeCustomizationService>();
        services.AddScoped<CacheService>();

        RegisterRepositories(services);

        services.AddScoped<DataSeeder>();

        return services;
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        var entityTypes = typeof(EntityBase).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.BaseType == typeof(EntityBase));

        foreach (var entityType in entityTypes)
        {
            var repoInterface = typeof(IRepository<,>).MakeGenericType(entityType, typeof(Guid));
            var repoImpl = typeof(EfRepository<,>).MakeGenericType(entityType, typeof(Guid));
            services.AddScoped(repoInterface, repoImpl);
        }
    }
}
