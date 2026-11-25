namespace YuktiraERP.Core.Domain.Common;

/// <summary>
/// Marker interface for all domain integration events.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAtUtc { get; }
    string EventType => GetType().Name;
}
