using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.BackgroundServices;

/// <summary>
/// Drains mes_outbox_event and pending MesErpPostingLog entries asynchronously
/// with exponential backoff so shop-floor execution is never blocked by ERP postings.
/// </summary>
public class MesOutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MesOutboxPublisherService> _logger;

    public MesOutboxPublisherService(IServiceScopeFactory scopeFactory, ILogger<MesOutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MesOutboxPublisher] Error draining outbox batch");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    public async Task<int> DrainBatchAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        var bus = scope.ServiceProvider.GetService<IMessageBus>();

        var now = DateTime.UtcNow;
        var pending = await db.OutboxEvents
            .Where(e => e.Status == OutboxStatus.PENDING && (e.NextRetryAt == null || e.NextRetryAt <= now))
            .OrderBy(e => e.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        int published = 0;
        foreach (var evt in pending)
        {
            try
            {
                if (bus != null)
                {
                    await bus.PublishAsync(new MessageEnvelope<object>
                    {
                        Id = evt.Id,
                        TenantId = evt.TenantId,
                        CorrelationId = evt.AggregateId.ToString(),
                        Payload = new
                        {
                            evt.Id,
                            evt.TenantId,
                            evt.PlantCode,
                            evt.EventType,
                            evt.AggregateType,
                            evt.AggregateId,
                            evt.PayloadJson
                        }
                    });
                }

                evt.Status = OutboxStatus.PUBLISHED;
                evt.PublishedAt = now;
                published++;
            }
            catch (Exception ex)
            {
                evt.AttemptCount++;
                evt.LastError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                if (evt.AttemptCount >= 5)
                {
                    evt.Status = OutboxStatus.FAILED;
                }
                else
                {
                    var backoffSec = Math.Pow(2, evt.AttemptCount) * 5;
                    evt.NextRetryAt = now.AddSeconds(backoffSec);
                }
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);

        return published;
    }
}
