using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Infrastructure.Services;

public class OutboxService : IOutboxService
{
    private readonly YuktiraDbContext _dbContext;
    private readonly ILogger<OutboxService>? _logger;

    public OutboxService(YuktiraDbContext dbContext, ILogger<OutboxService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task EnqueueAsync<TEvent>(
        TEvent domainEvent,
        string aggregateId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));

        var outboxMessage = new OutboxMessage
        {
            Id = domainEvent.EventId != Guid.Empty ? domainEvent.EventId : Guid.NewGuid(),
            TenantId = tenantId,
            OccurredAtUtc = domainEvent.OccurredAtUtc != default ? domainEvent.OccurredAtUtc : DateTime.UtcNow,
            EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
            AggregateId = aggregateId ?? string.Empty,
            PayloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            IsPublished = false,
            RetryCount = 0
        };

        await _dbContext.Set<OutboxMessage>().AddAsync(outboxMessage, cancellationToken);
        // Note: SaveChanges is either invoked by the caller within their transactional scope or here
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger?.LogDebug("Enqueued outbox message {MessageId} for event {EventType} (Aggregate: {AggregateId})",
            outboxMessage.Id, outboxMessage.EventType, aggregateId);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedMessagesAsync(
        int batchSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _dbContext.Set<OutboxMessage>()
                .Where(m => !m.IsPublished && m.RetryCount < 5)
                .OrderBy(m => m.OccurredAtUtc)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            return messages.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to query unpublished outbox messages");
            return Array.Empty<OutboxMessage>();
        }
    }

    public async Task MarkPublishedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<OutboxMessage>().FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.IsPublished = true;
            message.PublishedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger?.LogDebug("Marked outbox message {MessageId} as published.", messageId);
        }
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.Set<OutboxMessage>().FindAsync(new object[] { messageId }, cancellationToken);
        if (message != null)
        {
            message.RetryCount++;
            message.LastError = error;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger?.LogWarning("Outbox message {MessageId} failed (Attempt {RetryCount}): {Error}", messageId, message.RetryCount, error);
        }
    }
}
