using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class ApprovalWorkflowServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task SubmitPrForApprovalAsync_CreatesRequestAndSteps()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr = new PurchaseRequisitionEntity
        {
            TenantId = tenantId,
            PrNumber = "PR2026000100",
            Status = "DRAFT",
            TotalAmount = 5000m
        };
        db.PurchaseRequisitions.Add(pr);
        await db.SaveChangesAsync();

        var service = new ApprovalWorkflowService(db);
        var request = await service.SubmitPrForApprovalAsync(pr.Id, "user1");

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal("PR", request.Type);
        Assert.Equal("Pending", request.Status);

        var steps = await db.ApprovalSteps.Where(s => s.ApprovalRequestId == request.Id).ToListAsync();
        Assert.Equal(3, steps.Count);
        Assert.All(steps, s => Assert.Equal("Pending", s.Status));

        var updatedPr = await db.PurchaseRequisitions.FindAsync(pr.Id);
        Assert.Equal("PENDING_APPROVAL", updatedPr!.Status);
    }

    [Fact]
    public async Task SubmitPrForApprovalAsync_NonDraftStatus_Throws()
    {
        var db = CreateDb();
        var pr = new PurchaseRequisitionEntity
        {
            TenantId = Guid.NewGuid(),
            PrNumber = "PR2026000101",
            Status = "APPROVED"
        };
        db.PurchaseRequisitions.Add(pr);
        await db.SaveChangesAsync();

        var service = new ApprovalWorkflowService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitPrForApprovalAsync(pr.Id, "user1"));
    }

    [Fact]
    public async Task ApprovePrAsync_AllStepsApproved_SetsPrApproved()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr = new PurchaseRequisitionEntity
        {
            TenantId = tenantId,
            PrNumber = "PR2026000102",
            Status = "DRAFT",
            TotalAmount = 1000m
        };
        db.PurchaseRequisitions.Add(pr);
        await db.SaveChangesAsync();

        var service = new ApprovalWorkflowService(db);
        var request = await service.SubmitPrForApprovalAsync(pr.Id, "user1");

        var steps = await db.ApprovalSteps.Where(s => s.ApprovalRequestId == request.Id).OrderBy(s => s.Level).ToListAsync();

        var r1 = await service.ApprovePrAsync(request.Id, steps[0].Id, "approver1", "Looks good");
        Assert.True(r1.Success);

        var r2 = await service.ApprovePrAsync(request.Id, steps[1].Id, "approver2", "Approved");
        Assert.True(r2.Success);

        var r3 = await service.ApprovePrAsync(request.Id, steps[2].Id, "approver3", "Final approval");
        Assert.True(r3.Success);

        var updatedPr = await db.PurchaseRequisitions.FindAsync(pr.Id);
        Assert.Equal("APPROVED", updatedPr!.Status);

        var updatedRequest = await db.ApprovalRequests.FindAsync(request.Id);
        Assert.Equal("Approved", updatedRequest!.Status);
    }

    [Fact]
    public async Task RejectPrAsync_SetsPrRejected()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr = new PurchaseRequisitionEntity
        {
            TenantId = tenantId,
            PrNumber = "PR2026000103",
            Status = "DRAFT"
        };
        db.PurchaseRequisitions.Add(pr);
        await db.SaveChangesAsync();

        var service = new ApprovalWorkflowService(db);
        var request = await service.SubmitPrForApprovalAsync(pr.Id, "user1");

        var steps = await db.ApprovalSteps.Where(s => s.ApprovalRequestId == request.Id).OrderBy(s => s.Level).ToListAsync();

        var result = await service.RejectPrAsync(request.Id, steps[0].Id, "approver1", "Budget exceeded");
        Assert.True(result.Success);
        Assert.Equal("REJECTED", result.NewStatus);

        var updatedPr = await db.PurchaseRequisitions.FindAsync(pr.Id);
        Assert.Equal("REJECTED", updatedPr!.Status);
    }

    [Fact]
    public async Task ApprovePoAsync_AllStepsApproved_SetsPoApproved()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = "PO2026000100",
            Status = "DRAFT",
            TotalAmount = 10000m
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        var service = new ApprovalWorkflowService(db);
        var request = await service.SubmitPoForApprovalAsync(po.Id, "user1");

        var steps = await db.ApprovalSteps.Where(s => s.ApprovalRequestId == request.Id).OrderBy(s => s.Level).ToListAsync();

        await service.ApprovePoAsync(request.Id, steps[0].Id, "a1", "ok");
        await service.ApprovePoAsync(request.Id, steps[1].Id, "a2", "ok");
        await service.ApprovePoAsync(request.Id, steps[2].Id, "a3", "ok");

        var updatedPo = await db.PurchaseOrders.FindAsync(po.Id);
        Assert.Equal("APPROVED", updatedPo!.Status);
    }

    [Fact]
    public async Task RejectPoAsync_SetsPoRejected()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = "PO2026000101",
            Status = "DRAFT",
            TotalAmount = 8000m
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        var service = new ApprovalWorkflowService(db);
        var request = await service.SubmitPoForApprovalAsync(po.Id, "user1");

        var steps = await db.ApprovalSteps.Where(s => s.ApprovalRequestId == request.Id).OrderBy(s => s.Level).ToListAsync();

        var result = await service.RejectPoAsync(request.Id, steps[0].Id, "approver1", "Too expensive");
        Assert.True(result.Success);

        var updatedPo = await db.PurchaseOrders.FindAsync(po.Id);
        Assert.Equal("REJECTED", updatedPo!.Status);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_ReturnsPendingItems()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr = new PurchaseRequisitionEntity
        {
            TenantId = tenantId,
            PrNumber = "PR2026000104",
            Status = "DRAFT",
            TotalAmount = 2000m
        };
        db.PurchaseRequisitions.Add(pr);
        await db.SaveChangesAsync();

        var service = new ApprovalWorkflowService(db);
        await service.SubmitPrForApprovalAsync(pr.Id, "user1");

        var pending = await service.GetPendingApprovalsAsync("user1");

        Assert.Single(pending);
        Assert.Equal("PR", pending[0].DocumentType);
        Assert.Equal(2000m, pending[0].Amount);
    }

    [Fact]
    public async Task ApprovePrAsync_AlreadyActionedStep_ReturnsFalse()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr = new PurchaseRequisitionEntity
        {
            TenantId = tenantId,
            PrNumber = "PR2026000105",
            Status = "DRAFT"
        };
        db.PurchaseRequisitions.Add(pr);
        await db.SaveChangesAsync();

        var service = new ApprovalWorkflowService(db);
        var request = await service.SubmitPrForApprovalAsync(pr.Id, "user1");

        var steps = await db.ApprovalSteps.Where(s => s.ApprovalRequestId == request.Id).OrderBy(s => s.Level).ToListAsync();

        await service.ApprovePrAsync(request.Id, steps[0].Id, "a1", "ok");

        var result = await service.ApprovePrAsync(request.Id, steps[0].Id, "a1", "again");
        Assert.False(result.Success);
    }
}
