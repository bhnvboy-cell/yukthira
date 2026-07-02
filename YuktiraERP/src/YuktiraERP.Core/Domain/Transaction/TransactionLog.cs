namespace YuktiraERP.Core.Domain.Transaction;

public class TransactionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionCodeId { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Success;
    public string IpAddress { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RequestData { get; set; }
    public string? ResponseData { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
