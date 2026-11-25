using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Interfaces;

public interface IOutboxService
{
    Task EnqueueAsync<TEvent>(
        TEvent domainEvent,
        string aggregateId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default) where TEvent : IDomainEvent;

    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedMessagesAsync(
        int batchSize = 50,
        CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);
}
