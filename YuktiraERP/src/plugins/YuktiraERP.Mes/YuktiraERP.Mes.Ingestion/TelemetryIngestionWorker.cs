using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Ingestion;

public record TelemetryBatchItem(Guid TenantId, string PlantCode, string SourceId, MesTagValueRaw Sample);

/// <summary>
/// Separate Worker Service process for high-throughput telemetry ingestion (Section 1.1 & 1.4).
/// Uses a bounded channel for backpressure; overflow goes to mes_dead_letter.
/// </summary>
public class TelemetryIngestionWorker : BackgroundService
{
    private readonly Channel<TelemetryBatchItem> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelemetryIngestionWorker> _logger;

    public TelemetryIngestionWorker(IServiceScopeFactory scopeFactory, ILogger<TelemetryIngestionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _channel = Channel.CreateBounded<TelemetryBatchItem>(new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async ValueTask<bool> EnqueueAsync(TelemetryBatchItem item, CancellationToken ct = default)
    {
        if (_channel.Writer.TryWrite(item))
            return true;

        // Backpressure overflow -> dead-letter, never silent loss
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<MesDataCollectionService>();
        await svc.SendToDeadLetterAsync(item.TenantId, item.PlantCode, item.SourceId, item.Sample.MessageUuid, item.Sample, "Ingestion channel full (backpressure)", ct);
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new List<TelemetryBatchItem>(500);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await _channel.Reader.WaitToReadAsync(stoppingToken))
                {
                    while (buffer.Count < 500 && _channel.Reader.TryRead(out var item))
                        buffer.Add(item);

                    if (buffer.Count > 0)
                    {
                        await FlushBatchAsync(buffer, stoppingToken);
                        buffer.Clear();
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MesIngestion] Error flushing telemetry batch");
                buffer.Clear();
            }
        }
    }

    private async Task FlushBatchAsync(List<TelemetryBatchItem> batch, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<MesDataCollectionService>();

        foreach (var group in batch.GroupBy(b => (b.TenantId, b.PlantCode, b.SourceId)))
        {
            await svc.IngestTagBatchAsync(
                group.Key.TenantId,
                group.Key.PlantCode,
                group.Key.SourceId,
                group.Select(g => g.Sample),
                ct);
        }
    }
}
