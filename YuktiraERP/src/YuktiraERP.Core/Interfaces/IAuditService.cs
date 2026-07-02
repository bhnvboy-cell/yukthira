using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Interfaces;

public class AuditEntryDto
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public ActionType ActionType { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceInfo { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public bool IsSuspicious { get; set; }
    public string? Details { get; set; }
}

public interface IAuditService
{
    Task LogAsync(AuditEntryDto entry);
    Task<List<AuditEntryDto>> GetLogsAsync(Guid? tenantId, Guid? userId, string? module, ActionType? actionType, DateTime? from, DateTime? to, int page = 1, int pageSize = 50);
    Task<AuditEntryDto?> GetLogByIdAsync(Guid id);
    Task<int> GetLogCountAsync(Guid? tenantId);
    Task ExportToCsvAsync(Guid? tenantId, Stream stream);
    Task FlagSuspiciousAsync(Guid id);
    Task<int> DetectAndFlagSuspiciousAsync(Guid? tenantId = null);
    Task<List<AuditEntryDto>> GetFlaggedEntriesAsync(Guid? tenantId, int page = 1, int pageSize = 50);
}
