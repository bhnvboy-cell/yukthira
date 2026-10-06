using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.BackgroundServices;

public class SelfHealingReconciliationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<SelfHealingOptions> _options;
    private readonly ILogger<SelfHealingReconciliationJob> _logger;

    public SelfHealingReconciliationJob(
        IServiceScopeFactory scopeFactory,
        IOptions<SelfHealingOptions> options,
        ILogger<SelfHealingReconciliationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogInformation("Self-healing reconciliation job is disabled by configuration; not starting");
            return;
        }

        _logger.LogInformation(
            "Self-healing reconciliation job started (every {IntervalMinutes}m, stuck posting age {StuckMinutes}m)",
            options.IntervalMinutes, options.StuckPostingAgeMinutes);

        var delaySeconds = options.StartupDelaySeconds < 0 ? 0 : options.StartupDelaySeconds;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var interval = TimeSpan.FromMinutes(options.IntervalMinutes <= 0 ? 60 : options.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<ISelfHealingReconciliationService>();

                var tenantIds = await db.Tenants
                    .AsNoTracking()
                    .Select(t => t.Id)
                    .ToListAsync(stoppingToken);

                if (tenantIds.Count == 0)
                {
                    tenantIds.Add(Guid.Empty);
                }

                foreach (var tenantId in tenantIds)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        var result = await service.RunOnceAsync(tenantId, "system", stoppingToken);
                        _logger.LogInformation(
                            "Self-healing run for tenant {TenantId}: applied={Applied}, skipped={Skipped}, flagged={Flagged}, failed={Failed}",
                            tenantId, result.AppliedCount, result.SkippedCount, result.FlaggedCount, result.FailedCount);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Self-healing run failed for tenant {TenantId}", tenantId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Self-healing reconciliation job cycle failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
