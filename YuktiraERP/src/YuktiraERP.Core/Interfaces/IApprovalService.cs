namespace YuktiraERP.Core.Interfaces;

public class ApprovalRequestDto
{
    public Guid Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public int CurrentLevel { get; set; }
    public int MaxLevel { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public interface IApprovalService
{
    Task<Guid> CreateApprovalRequestAsync(Guid tenantId, string module, string documentType, string documentId, string documentNumber, decimal amount, Guid requestedBy);
    Task<bool> ApproveAsync(Guid approvalRequestId, Guid approverId, string? comments = null);
    Task<bool> RejectAsync(Guid approvalRequestId, Guid approverId, string reason);
    Task<bool> EscalateAsync(Guid approvalRequestId);
    Task<List<ApprovalRequestDto>> GetPendingApprovalsAsync(Guid tenantId, Guid userId);
    Task<ApprovalRequestDto?> GetApprovalByIdAsync(Guid id);
}
