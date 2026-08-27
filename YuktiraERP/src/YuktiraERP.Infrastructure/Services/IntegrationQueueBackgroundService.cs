using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

/// <summary>
/// Processes the outbound integration queue using message bus for event-driven processing.
/// Runs every 30 seconds as fallback, but primarily processes via IMessageBus.
/// </summary>
public class IntegrationQueueBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrationQueueBackgroundService> _logger;
    private readonly IMessageBus _messageBus;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    public IntegrationQueueBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<IntegrationQueueBackgroundService> logger,
        IMessageBus messageBus)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _messageBus = messageBus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Integration queue background processor started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Integration queue background processing failed");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();
        var queue = scope.ServiceProvider.GetRequiredService<IIntegrationQueueService>();

        var tenantIds = await db.Tenants
            .Where(t => t.Status == "ACTIVE")
            .Select(t => t.Id)
            .ToListAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            if (ct.IsCancellationRequested) return;
            var pending = await queue.GetPendingAsync(tenantId, 50);
            if (pending.Count == 0) continue;
            await queue.ProcessQueueAsync(tenantId);
        }
    }
}

/// <summary>
/// Runs MRP shortage detection once per day (configurable interval) for every
/// active tenant, so the UI never has to be visited for the scheduler to fire.
/// </summary>
public class MrpSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MrpSchedulerBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public MrpSchedulerBackgroundService(IServiceScopeFactory scopeFactory, ILogger<MrpSchedulerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MRP daily scheduler started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunMrpForAllTenantsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled MRP run failed");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunMrpForAllTenantsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();
        var mrp = scope.ServiceProvider.GetRequiredService<IMrpService>();

        var tenantIds = await db.Tenants
            .Where(t => t.Status == "ACTIVE")
            .Select(t => t.Id)
            .ToListAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            if (ct.IsCancellationRequested) return;
            try
            {
                await mrp.RunMrpAsync(tenantId);
                _logger.LogInformation("Scheduled MRP completed for tenant {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled MRP failed for tenant {TenantId}", tenantId);
            }
        }
    }
}