using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IApprovalWorkflowService
{
    Task<ApprovalRequestEntity> SubmitPrForApprovalAsync(Guid prId, string userId);
    Task<ApprovalRequestEntity> SubmitPoForApprovalAsync(Guid poId, string userId);
    Task<ApprovalResult> ApprovePrAsync(Guid requestId, Guid stepId, string userId, string comments);
    Task<ApprovalResult> RejectPrAsync(Guid requestId, Guid stepId, string userId, string comments);
    Task<ApprovalResult> ApprovePoAsync(Guid requestId, Guid stepId, string userId, string comments);
    Task<ApprovalResult> RejectPoAsync(Guid requestId, Guid stepId, string userId, string comments);
    Task<List<PendingApprovalDto>> GetPendingApprovalsAsync(string userId);
}

public class ApprovalResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string NewStatus { get; set; } = "";
}

public class PendingApprovalDto
{
    public Guid RequestId { get; set; }
    public string DocumentType { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Subject { get; set; } = "";
    public decimal Amount { get; set; }
    public int CurrentLevel { get; set; }
    public int MaxLevel { get; set; } = 3;
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly YuktiraDbContext _db;

    public ApprovalWorkflowService(YuktiraDbContext db) { _db = db; }

    public async Task<ApprovalRequestEntity> SubmitPrForApprovalAsync(Guid prId, string userId)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(prId)
            ?? throw new InvalidOperationException("Purchase Requisition not found.");

        if (pr.Status != "DRAFT" && pr.Status != "Pending")
            throw new InvalidOperationException("PR must be in DRAFT or Pending status to submit for approval.");

        var tenantId = pr.TenantId != Guid.Empty ? pr.TenantId : _db.TenantId ?? Guid.Empty;

        var request = new ApprovalRequestEntity
        {
            TenantId = tenantId,
            RequestId = $"APPR-{DateTime.Now:yyyyMMdd}-{pr.PrNumber}",
            Type = "PR",
            Subject = $"Purchase Requisition {pr.PrNumber}",
            Requestor = userId,
            RequestDate = DateTime.UtcNow,
            Amount = pr.TotalAmount > 0 ? pr.TotalAmount : pr.Amount,
            Status = "Pending"
        };

        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync();

        for (int level = 1; level <= 3; level++)
        {
            _db.ApprovalSteps.Add(new ApprovalStepEntity
            {
                TenantId = tenantId,
                ApprovalRequestId = request.Id,
                Level = level,
                ApproverName = $"Level {level} Approver",
                ApproverUserId = "",
                Status = "Pending"
            });
        }

        await _db.SaveChangesAsync();

        pr.Status = "PENDING_APPROVAL";
        pr.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return request;
    }

    public async Task<ApprovalRequestEntity> SubmitPoForApprovalAsync(Guid poId, string userId)
    {
        var po = await _db.PurchaseOrders.FindAsync(poId)
            ?? throw new InvalidOperationException("Purchase Order not found.");

        if (po.Status != "DRAFT" && po.Status != "Pending Approval")
            throw new InvalidOperationException("PO must be in DRAFT or Pending Approval status to submit for approval.");

        var tenantId = po.TenantId != Guid.Empty ? po.TenantId : _db.TenantId ?? Guid.Empty;

        var request = new ApprovalRequestEntity
        {
            TenantId = tenantId,
            RequestId = $"APPO-{DateTime.Now:yyyyMMdd}-{po.PoNumber}",
            Type = "PO",
            Subject = $"Purchase Order {po.PoNumber}",
            Requestor = userId,
            RequestDate = DateTime.UtcNow,
            Amount = po.TotalAmount > 0 ? po.TotalAmount : po.Amount,
            Status = "Pending"
        };

        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync();

        for (int level = 1; level <= 3; level++)
        {
            _db.ApprovalSteps.Add(new ApprovalStepEntity
            {
                TenantId = tenantId,
                ApprovalRequestId = request.Id,
                Level = level,
                ApproverName = $"Level {level} Approver",
                ApproverUserId = "",
                Status = "Pending"
            });
        }

        await _db.SaveChangesAsync();

        po.Status = "PENDING_APPROVAL";
        po.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return request;
    }

    public async Task<ApprovalResult> ApprovePrAsync(Guid requestId, Guid stepId, string userId, string comments)
    {
        var request = await _db.ApprovalRequests.FindAsync(requestId);
        if (request == null || request.Type != "PR")
            return new ApprovalResult { Success = false, Message = "Approval request not found." };

        var step = await _db.ApprovalSteps.FindAsync(stepId);
        if (step == null || step.ApprovalRequestId != requestId)
            return new ApprovalResult { Success = false, Message = "Approval step not found." };

        if (step.Status != "Pending")
            return new ApprovalResult { Success = false, Message = "Step already actioned." };

        step.Status = "Approved";
        step.Comments = comments;
        step.ActionedAt = DateTime.UtcNow;
        step.ApproverUserId = userId;

        var allSteps = await _db.ApprovalSteps
            .Where(s => s.ApprovalRequestId == requestId)
            .OrderBy(s => s.Level)
            .ToListAsync();

        var pendingSteps = allSteps.Where(s => s.Status == "Pending").ToList();
        var approvedCount = allSteps.Count(s => s.Status == "Approved");

        if (pendingSteps.Count == 0 && approvedCount == allSteps.Count)
        {
            request.Status = "Approved";
            var prNumber = request.RequestId.Replace($"APPR-{DateTime.Now:yyyyMMdd}-", "");
            var pr = await _db.PurchaseRequisitions.FirstOrDefaultAsync(p => p.PrNumber == prNumber);
            if (pr != null)
            {
                pr.Status = "APPROVED";
                pr.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            request.Status = "In Progress";
        }

        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ApprovalResult { Success = true, Message = "Approved.", NewStatus = request.Status };
    }

    public async Task<ApprovalResult> RejectPrAsync(Guid requestId, Guid stepId, string userId, string comments)
    {
        var request = await _db.ApprovalRequests.FindAsync(requestId);
        if (request == null || request.Type != "PR")
            return new ApprovalResult { Success = false, Message = "Approval request not found." };

        var step = await _db.ApprovalSteps.FindAsync(stepId);
        if (step == null || step.ApprovalRequestId != requestId)
            return new ApprovalResult { Success = false, Message = "Approval step not found." };

        step.Status = "Rejected";
        step.Comments = comments;
        step.ActionedAt = DateTime.UtcNow;
        step.ApproverUserId = userId;

        request.Status = "Rejected";
        request.UpdatedAt = DateTime.UtcNow;

        var prNumber = request.RequestId.Replace($"APPR-{DateTime.Now:yyyyMMdd}-", "");
        var pr = await _db.PurchaseRequisitions.FirstOrDefaultAsync(p => p.PrNumber == prNumber);
        if (pr != null)
        {
            pr.Status = "REJECTED";
            pr.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return new ApprovalResult { Success = true, Message = "Rejected.", NewStatus = "REJECTED" };
    }

    public async Task<ApprovalResult> ApprovePoAsync(Guid requestId, Guid stepId, string userId, string comments)
    {
        var request = await _db.ApprovalRequests.FindAsync(requestId);
        if (request == null || request.Type != "PO")
            return new ApprovalResult { Success = false, Message = "Approval request not found." };

        var step = await _db.ApprovalSteps.FindAsync(stepId);
        if (step == null || step.ApprovalRequestId != requestId)
            return new ApprovalResult { Success = false, Message = "Approval step not found." };

        if (step.Status != "Pending")
            return new ApprovalResult { Success = false, Message = "Step already actioned." };

        step.Status = "Approved";
        step.Comments = comments;
        step.ActionedAt = DateTime.UtcNow;
        step.ApproverUserId = userId;

        var allSteps = await _db.ApprovalSteps
            .Where(s => s.ApprovalRequestId == requestId)
            .OrderBy(s => s.Level)
            .ToListAsync();

        var pendingSteps = allSteps.Where(s => s.Status == "Pending").ToList();
        var approvedCount = allSteps.Count(s => s.Status == "Approved");

        if (pendingSteps.Count == 0 && approvedCount == allSteps.Count)
        {
            request.Status = "Approved";
            var poNumber = request.RequestId.Replace($"APPO-{DateTime.Now:yyyyMMdd}-", "");
            var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.PoNumber == poNumber);
            if (po != null)
            {
                po.Status = "APPROVED";
                po.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            request.Status = "In Progress";
        }

        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ApprovalResult { Success = true, Message = "Approved.", NewStatus = request.Status };
    }

    public async Task<ApprovalResult> RejectPoAsync(Guid requestId, Guid stepId, string userId, string comments)
    {
        var request = await _db.ApprovalRequests.FindAsync(requestId);
        if (request == null || request.Type != "PO")
            return new ApprovalResult { Success = false, Message = "Approval request not found." };

        var step = await _db.ApprovalSteps.FindAsync(stepId);
        if (step == null || step.ApprovalRequestId != requestId)
            return new ApprovalResult { Success = false, Message = "Approval step not found." };

        step.Status = "Rejected";
        step.Comments = comments;
        step.ActionedAt = DateTime.UtcNow;
        step.ApproverUserId = userId;

        request.Status = "Rejected";
        request.UpdatedAt = DateTime.UtcNow;

        var poNumber = request.RequestId.Replace($"APPO-{DateTime.Now:yyyyMMdd}-", "");
        var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.PoNumber == poNumber);
        if (po != null)
        {
            po.Status = "REJECTED";
            po.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return new ApprovalResult { Success = true, Message = "Rejected.", NewStatus = "REJECTED" };
    }

    public async Task<List<PendingApprovalDto>> GetPendingApprovalsAsync(string userId)
    {
        var requests = await _db.ApprovalRequests
            .Where(a => a.Status == "Pending" || a.Status == "In Progress")
            .OrderByDescending(a => a.RequestDate)
            .ToListAsync();

        return requests.Select(a => new PendingApprovalDto
        {
            RequestId = a.Id,
            DocumentType = a.Type,
            DocumentNumber = a.RequestId,
            Subject = a.Subject,
            Amount = a.Amount ?? 0,
            CurrentLevel = _db.ApprovalSteps.Count(s => s.ApprovalRequestId == a.Id && s.Status == "Pending") > 0
                ? _db.ApprovalSteps.Where(s => s.ApprovalRequestId == a.Id && s.Status == "Approved").Count() + 1
                : 3,
            Status = a.Status,
            CreatedAt = a.RequestDate
        }).ToList();
    }
}
