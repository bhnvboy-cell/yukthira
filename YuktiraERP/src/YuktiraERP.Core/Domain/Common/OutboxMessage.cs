namespace YuktiraERP.Core.Domain.Common;

/// <summary>
/// Persisted integration event in the Transactional Outbox pattern.
/// Maps to PostgreSQL table: outbox.messages.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAtUtc { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
}
