namespace YuktiraERP.Core.Domain.Audit;

/// <summary>
/// Implemented by domain entities that participate in automated audit tracking.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    Guid? CreatedByUserId { get; set; }
    DateTime? LastModifiedAtUtc { get; set; }
    Guid? LastModifiedByUserId { get; set; }
}

/// <summary>
/// Detailed field-level difference record for an audit event.
/// </summary>
public class FieldDifference
{
    public object? OriginalValue { get; set; }
    public object? CurrentValue { get; set; }
}

/// <summary>
/// Persisted audit change log entry representing an atomic mutation to a domain entity.
/// Partitioned by period in PostgreSQL (audit.change_logs).
/// </summary>
public class AuditChangeLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string EntityType { get; set; } = string.Empty;
    public string EntityPk { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Insert, Update, Delete
    public Guid? ActorUserId { get; set; }
    public string? IpAddress { get; set; }
    public string ChangedFieldsJson { get; set; } = "{}";
    public Guid? TenantId { get; set; }
}

/// <summary>
/// User context abstraction for capturing current actor in interceptors.
/// </summary>
public interface ICurrentActorContext
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    string? IpAddress { get; }
}
