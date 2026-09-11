using YuktiraERP.Core.Enums;

namespace YuktiraERP.Core.Dtos;

public class DomainEventEnvelope
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid AggregateId { get; set; }
    public AggregateType AggregateType { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    public string? UserId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class EventReplayRequest
{
    public Guid TenantId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public AggregateType? AggregateType { get; set; }
    public string? EventType { get; set; }
    public int? MaxEvents { get; set; }
}

public class EventReplayResult
{
    public bool Success { get; set; }
    public int TotalEventsReplayed { get; set; }
    public List<string> ProjectedModels { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ReadModelSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ModelName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string ModelData { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ProjectionStatus Status { get; set; } = ProjectionStatus.Current;
}
