using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Messaging;
using YuktiraERP.Infrastructure.MultiTenant;
using YuktiraERP.Infrastructure.Security;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Infrastructure.Services.Connectors;
using YuktiraERP.Infrastructure.Caching;
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddYuktiraInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("YuktiraDb");
        services.AddScoped<TenantSaveChangesInterceptor>();
        if (!string.IsNullOrEmpty(connStr))
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            services.AddDbContext<YuktiraDbContext>((sp, options) => options
                .UseNpgsql(connStr)
                .AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>()));
        }
        else
        {
            services.AddDbContext<YuktiraDbContext>((sp, options) => options
                .UseInMemoryDatabase("YuktiraERP")
                .AddInterceptors(sp.GetRequiredService<TenantSaveChangesInterceptor>()));
        }

        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddHttpContextAccessor();

        services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        services.AddHostedService<MessageBusConsumerService>();
        services.AddHostedService<IntegrationQueueBackgroundService>();
        services.AddHostedService<MrpSchedulerBackgroundService>();

        var encryptionKey = configuration["Encryption:Key"];
        if (!string.IsNullOrEmpty(encryptionKey))
        {
            services.AddSingleton<IEncryptionService>(sp => new AesEncryptionService(encryptionKey));
        }
        else
        {
            services.AddSingleton<IEncryptionService>(sp => new AesEncryptionService(Convert.ToBase64String(new byte[32])));
        }

        // Distributed Caching - Try Redis first, fall back to in-memory
        services.Configure<RedisCacheOptions>(configuration.GetSection("Redis"));
        var redisConnection = configuration["Redis:Connection"];
        if (!string.IsNullOrEmpty(redisConnection))
        {
            try
            {
                var opts = StackExchange.Redis.ConfigurationOptions.Parse(redisConnection);
                opts.AbortOnConnectFail = false;
                var conn = StackExchange.Redis.ConnectionMultiplexer.Connect(opts);
                if (conn.IsConnected)
                {
                    services.AddSingleton<IDistributedCacheService, RedisCacheService>();
                }
                else
                {
                    services.AddSingleton<IDistributedCacheService, MemoryCacheFallbackService>();
                }
                conn.Dispose();
            }
            catch
            {
                services.AddSingleton<IDistributedCacheService, MemoryCacheFallbackService>();
            }
        }
        else
        {
            services.AddSingleton<IDistributedCacheService, MemoryCacheFallbackService>();
        }

        services.AddSingleton<IModuleCatalog, ModuleCatalog>();

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<ISmsSender, TwilioSmsSender>();
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
            reg.Register(new SapHanaConnector(http));
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
        services.AddScoped<ITaxService, TaxService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<ICostAllocationService, CostAllocationService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<ITransactionCodeService, TransactionCodeService>();
        services.AddScoped<IWorkflowEngine, WorkflowService>();
        services.AddScoped<ISuperUserService, SuperUserService>();
        services.AddScoped<IThemeService, ThemeService>();
        services.AddScoped<ITCodeGeneratorService, TCodeGeneratorService>();
        services.AddScoped<ITCodeCustomizationService, TCodeCustomizationService>();
        services.AddScoped<IBatchService, BatchService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IGoodsMovementService, GoodsMovementService>();
        services.AddScoped<YuktiraERP.Core.Interfaces.IMovementTypeEngineService, MovementTypeEngineService>();
        services.AddScoped<YuktiraERP.Infrastructure.Services.IMovementTypeEngineService, MovementTypeEngineService>();
        services.AddScoped<IProductionOrderService, ProductionOrderService>();
        services.AddScoped<IBankService, BankService>();
        services.AddScoped<IDepartmentKeyService, DepartmentKeyService>();
        services.AddScoped<IReleaseStrategyService, ReleaseStrategyService>();
        services.AddScoped<IPrPoConversionService, PrPoConversionService>();
        services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
        services.AddScoped<IThreeWayMatchService, ThreeWayMatchService>();
        services.AddScoped<CacheService>();
        services.AddHostedService<IntegrationQueueBackgroundService>();
        services.AddHostedService<MrpSchedulerBackgroundService>();

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
