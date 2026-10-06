using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.BackgroundServices;

public class ParquetExportWorker : BackgroundService
{
    private readonly IColumnarJournalCache _cache;
    private readonly IOptions<ColumnarCacheOptions> _options;
    private readonly ILogger<ParquetExportWorker>? _logger;

    public ParquetExportWorker(
        IColumnarJournalCache cache,
        IOptions<ColumnarCacheOptions> options,
        ILogger<ParquetExportWorker>? logger = null)
    {
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger?.LogInformation("Parquet export worker is disabled by configuration; not starting");
            return;
        }

        var refresh = TimeSpan.FromMinutes(Math.Max(1, options.RefreshMinutes));
        var exportEvery = TimeSpan.FromMinutes(Math.Max(1, options.ExportMinutes));
        var lastExport = DateTime.MinValue;

        _logger?.LogInformation(
            "Parquet export worker started (refresh every {RefreshMinutes}m, export every {ExportMinutes}m into {ExportDirectory})",
            options.RefreshMinutes, options.ExportMinutes, options.ExportDirectory);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var rows = await _cache.RefreshAsync(null, stoppingToken);
                _logger?.LogDebug("Columnar journal cache refreshed with {Rows} rows", rows);

                if (DateTime.UtcNow - lastExport >= exportEvery)
                {
                    var path = await _cache.ExportAsync(null, stoppingToken);
                    lastExport = DateTime.UtcNow;
                    _logger?.LogInformation("Columnar journal cache exported to {Path}", path);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Columnar refresh or parquet export cycle failed");
            }

            try
            {
                await Task.Delay(refresh, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger?.LogInformation("Parquet export worker stopped");
    }
}
