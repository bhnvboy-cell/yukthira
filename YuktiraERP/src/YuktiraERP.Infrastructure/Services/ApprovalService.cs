using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class ApprovalService : IApprovalService
{
    private static readonly List<ApprovalRequestDto> _approvals = new();

    public Task<Guid> CreateApprovalRequestAsync(Guid tenantId, string module, string documentType, string documentId, string documentNumber, decimal amount, Guid requestedBy)
    {
        var id = Guid.NewGuid();
        _approvals.Add(new ApprovalRequestDto
        {
            Id = id,
            Module = module,
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            Amount = amount,
            RequestedByName = requestedBy.ToString(),
            CurrentLevel = 1,
            MaxLevel = 3,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        });
        return Task.FromResult(id);
    }

    public Task<bool> ApproveAsync(Guid approvalRequestId, Guid approverId, string? comments = null)
    {
        var req = _approvals.FirstOrDefault(a => a.Id == approvalRequestId);
        if (req != null)
        {
            req.CurrentLevel++;
            if (req.CurrentLevel > req.MaxLevel)
                req.Status = "APPROVED";
        }
        return Task.FromResult(req != null);
    }

    public Task<bool> RejectAsync(Guid approvalRequestId, Guid approverId, string reason)
    {
        var req = _approvals.FirstOrDefault(a => a.Id == approvalRequestId);
        if (req != null) req.Status = "REJECTED";
        return Task.FromResult(req != null);
    }

    public Task<bool> EscalateAsync(Guid approvalRequestId)
    {
        var req = _approvals.FirstOrDefault(a => a.Id == approvalRequestId);
        if (req != null) req.Status = "ESCALATED";
        return Task.FromResult(req != null);
    }

    public Task<List<ApprovalRequestDto>> GetPendingApprovalsAsync(Guid tenantId, Guid userId)
        => Task.FromResult(_approvals.Where(a => a.Status == "PENDING").ToList());

    public Task<ApprovalRequestDto?> GetApprovalByIdAsync(Guid id)
        => Task.FromResult(_approvals.FirstOrDefault(a => a.Id == id));
}
