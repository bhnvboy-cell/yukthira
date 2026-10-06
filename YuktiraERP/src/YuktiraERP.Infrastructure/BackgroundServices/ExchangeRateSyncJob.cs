using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.BackgroundServices;

/// <summary>
/// Hosted job that runs the exchange rate sync on startup (after a configurable delay) and then
/// every IntervalHours. Registered by both hosts, but ExchangeRateSync:Enabled is false in the Web
/// host appsettings, so only the API host executes the loop.
/// </summary>
public class ExchangeRateSyncJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ExchangeRateSyncOptions> _options;
    private readonly IExchangeRateSyncStatusStore _status;
    private readonly ILogger<ExchangeRateSyncJob> _logger;

    public ExchangeRateSyncJob(
        IServiceScopeFactory scopeFactory,
        IOptions<ExchangeRateSyncOptions> options,
        IExchangeRateSyncStatusStore status,
        ILogger<ExchangeRateSyncJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _status = status;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogInformation("Exchange rate sync job is disabled by configuration; not starting");
            return;
        }

        _logger.LogInformation(
            "Exchange rate sync job started (provider {Provider}, base {BaseCurrency}, every {IntervalHours}h)",
            options.Provider, options.BaseCurrency, options.IntervalHours);

        var delaySeconds = options.StartupDelaySeconds < 0 ? 0 : options.StartupDelaySeconds;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var interval = TimeSpan.FromHours(options.IntervalHours <= 0 ? 24 : options.IntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_status.IsRunning)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IExchangeRateSyncService>();
                    var result = await service.SyncAsync(stoppingToken);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Exchange rate sync completed via {Provider}: {Fetched} rates fetched, {Inserted} inserted, {Updated} updated",
                            result.Provider, result.RatesFetched, result.Inserted, result.Updated);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Exchange rate sync finished without success: {Errors}",
                            string.Join(" | ", result.Errors));
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // The job must never take the host down.
                    _logger.LogError(ex, "Exchange rate sync job run failed");
                }
            }
            else
            {
                _logger.LogInformation("Exchange rate sync already running; skipping this scheduled run");
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
