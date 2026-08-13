using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ApprovalService : IApprovalService
{
    private readonly YuktiraDbContext _db;

    public ApprovalService(YuktiraDbContext db) { _db = db; }

    public async Task<Guid> CreateApprovalRequestAsync(Guid tenantId, string module, string documentType, string documentId, string documentNumber, decimal amount, Guid requestedBy)
    {
        var entity = new ApprovalRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequestId = $"APPROVAL-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..22],
            Type = documentType,
            Subject = $"{module} {documentType} {documentNumber}",
            Requestor = requestedBy.ToString(),
            RequestDate = DateTime.UtcNow,
            Amount = amount,
            Status = "Pending"
        };
        _db.ApprovalRequests.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> ApproveAsync(Guid approvalRequestId, Guid approverId, string? comments = null)
    {
        var req = await _db.ApprovalRequests.FindAsync(approvalRequestId);
        if (req == null) return false;
        if (req.Status == "Approved" || req.Status == "Rejected") return false;

        var level = (await _db.ApprovalSteps.CountAsync(s => s.ApprovalRequestId == req.Id)) + 1;
        _db.ApprovalSteps.Add(new ApprovalStepEntity
        {
            TenantId = req.TenantId,
            ApprovalRequestId = req.Id,
            Level = level,
            ApproverName = approverId.ToString(),
            Status = "Approved",
            Comments = comments ?? "",
            ActionedAt = DateTime.UtcNow
        });

        req.Status = level >= 3 ? "Approved" : "Pending";
        req.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(Guid approvalRequestId, Guid approverId, string reason)
    {
        var req = await _db.ApprovalRequests.FindAsync(approvalRequestId);
        if (req == null) return false;

        req.Status = "Rejected";
        req.UpdatedAt = DateTime.UtcNow;
        _db.ApprovalSteps.Add(new ApprovalStepEntity
        {
            TenantId = req.TenantId,
            ApprovalRequestId = req.Id,
            Level = (await _db.ApprovalSteps.CountAsync(s => s.ApprovalRequestId == req.Id)) + 1,
            ApproverName = approverId.ToString(),
            Status = "Rejected",
            Comments = reason,
            ActionedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EscalateAsync(Guid approvalRequestId)
    {
        var req = await _db.ApprovalRequests.FindAsync(approvalRequestId);
        if (req == null) return false;

        req.Status = "Escalated";
        req.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ApprovalRequestDto>> GetPendingApprovalsAsync(Guid tenantId, Guid userId)
    {
        var requests = await _db.ApprovalRequests
            .Where(a => a.TenantId == tenantId && a.Status == "Pending")
            .OrderByDescending(a => a.RequestDate)
            .ToListAsync();

        return requests.Select(a => new ApprovalRequestDto
        {
            Id = a.Id,
            Module = a.Subject.Split(' ').FirstOrDefault() ?? "",
            DocumentType = a.Type,
            DocumentNumber = a.Subject,
            Amount = a.Amount ?? 0,
            RequestedByName = a.Requestor,
            CurrentLevel = _db.ApprovalSteps.Count(s => s.ApprovalRequestId == a.Id) + 1,
            MaxLevel = 3,
            Status = a.Status,
            CreatedAt = a.RequestDate
        }).ToList();
    }

    public async Task<ApprovalRequestDto?> GetApprovalByIdAsync(Guid id)
    {
        var a = await _db.ApprovalRequests.FindAsync(id);
        if (a == null) return null;

        return new ApprovalRequestDto
        {
            Id = a.Id,
            Module = a.Subject.Split(' ').FirstOrDefault() ?? "",
            DocumentType = a.Type,
            DocumentNumber = a.Subject,
            Amount = a.Amount ?? 0,
            RequestedByName = a.Requestor,
            CurrentLevel = await _db.ApprovalSteps.CountAsync(s => s.ApprovalRequestId == a.Id) + 1,
            MaxLevel = 3,
            Status = a.Status,
            CreatedAt = a.RequestDate
        };
    }
}