using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.BackgroundServices;

public class FinancialEventStreamProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<FinancialEventStreamOptions> _options;
    private readonly ILogger<FinancialEventStreamProcessor>? _logger;

    public FinancialEventStreamProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<FinancialEventStreamOptions> options,
        ILogger<FinancialEventStreamProcessor>? logger = null)
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
            _logger?.LogInformation("Financial event stream processor is disabled by configuration; not starting");
            return;
        }

        var interval = TimeSpan.FromMilliseconds(Math.Max(100, options.FlushMilliseconds));
        _logger?.LogInformation(
            "Financial event stream processor started (flush {FlushMilliseconds}ms, batch size {BatchSize})",
            options.FlushMilliseconds, options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var stream = scope.ServiceProvider.GetRequiredService<IFinancialEventStream>();
                var drained = await stream.DrainPendingAsync(stoppingToken);
                if (drained > 0)
                {
                    _logger?.LogDebug("Financial event stream drained {Count} pending events", drained);
                    continue;
                }

                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Financial event stream processing cycle failed");
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

        _logger?.LogInformation("Financial event stream processor stopped");
    }
}
