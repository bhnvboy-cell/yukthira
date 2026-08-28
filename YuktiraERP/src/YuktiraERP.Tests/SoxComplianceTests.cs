using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Tests;

public class SoxComplianceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task SOX01_AssignDuty_Success()
    {
        var db = CreateDb();
        var service = new SoxComplianceService(db);
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var result = await service.AssignDutyAsync(new DutyAssignmentRequest
        {
            UserId = userId,
            DutyType = "PO_APPROVAL",
            Role = "Manager",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(6),
            Description = "Purchase Order Approval duty",
            AssignedByUserId = adminId
        });

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.AssignmentId);
        Assert.Contains("PO_APPROVAL", result.Message);

        var stored = await db.SoxAssignments.FindAsync(result.AssignmentId);
        Assert.NotNull(stored);
        Assert.Equal(userId.ToString(), stored.UserId);
        Assert.Equal("PO_APPROVAL", stored.DutyCode);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task SOX02_LogAuditTrail_ImmutableHash()
    {
        var db = CreateDb();
        var service = new SoxComplianceService(db);
        var entityId = Guid.NewGuid();

        var entry1 = await service.LogAuditTrailAsync(new AuditTrailLogRequest
        {
            EntityType = "PurchaseOrder",
            EntityId = entityId,
            Action = "CREATE",
            UserId = Guid.NewGuid(),
            UserName = "testuser",
            OldValues = null,
            NewValues = new System.Collections.Generic.Dictionary<string, string> { ["Status"] = "Created" }
        });

        Assert.True(entry1.Success);
        Assert.NotEqual(Guid.Empty, entry1.AuditEntryId);

        var entry2 = await service.LogAuditTrailAsync(new AuditTrailLogRequest
        {
            EntityType = "PurchaseOrder",
            EntityId = entityId,
            Action = "UPDATE",
            UserId = Guid.NewGuid(),
            UserName = "testuser",
            OldValues = new System.Collections.Generic.Dictionary<string, string> { ["Status"] = "Created" },
            NewValues = new System.Collections.Generic.Dictionary<string, string> { ["Status"] = "Approved" }
        });

        Assert.True(entry2.Success);

        var storedEntry1 = await db.ImmutableAuditTrails.FindAsync(entry1.AuditEntryId);
        var storedEntry2 = await db.ImmutableAuditTrails.FindAsync(entry2.AuditEntryId);
        Assert.NotNull(storedEntry1);
        Assert.NotNull(storedEntry2);
        Assert.Equal(1, storedEntry1.SequenceNumber);
        Assert.Equal(2, storedEntry2.SequenceNumber);
        Assert.NotEmpty(storedEntry1.CurrentHash);
        Assert.Equal(storedEntry1.CurrentHash, storedEntry2.PreviousHash);
        Assert.True(storedEntry1.IsImmutable);
    }

    [Fact]
    public async Task SOX03_VerifyAuditIntegrity_Valid()
    {
        var db = CreateDb();
        var service = new SoxComplianceService(db);
        var entityId = Guid.NewGuid();

        await service.LogAuditTrailAsync(new AuditTrailLogRequest
        {
            EntityType = "PurchaseOrder",
            EntityId = entityId,
            Action = "CREATE",
            UserId = Guid.NewGuid(),
            UserName = "user1"
        });

        await service.LogAuditTrailAsync(new AuditTrailLogRequest
        {
            EntityType = "PurchaseOrder",
            EntityId = entityId,
            Action = "UPDATE",
            UserId = Guid.NewGuid(),
            UserName = "user1"
        });

        await service.LogAuditTrailAsync(new AuditTrailLogRequest
        {
            EntityType = "PurchaseOrder",
            EntityId = entityId,
            Action = "APPROVE",
            UserId = Guid.NewGuid(),
            UserName = "user2"
        });

        var auditTrail = await service.GetAuditTrailAsync(new AuditTrailQueryRequest
        {
            EntityType = "PurchaseOrder",
            PageSize = 10
        });

        Assert.Equal(3, auditTrail.TotalCount);
        Assert.Equal(3, auditTrail.Entries.Count);

        var entries = await db.ImmutableAuditTrails
            .Where(a => a.TableName == "PurchaseOrder")
            .OrderBy(a => a.SequenceNumber)
            .ToListAsync();

        Assert.Equal(3, entries.Count);
        Assert.Equal(1, entries[0].SequenceNumber);
        Assert.Equal(2, entries[1].SequenceNumber);
        Assert.Equal(3, entries[2].SequenceNumber);
        Assert.NotEmpty(entries[0].CurrentHash);
        Assert.Equal(entries[0].CurrentHash, entries[1].PreviousHash);
        Assert.Equal(entries[1].CurrentHash, entries[2].PreviousHash);
    }

    [Fact]
    public async Task SOX04_DetectDutyConflict()
    {
        var db = CreateDb();
        var service = new SoxComplianceService(db);

        db.SoxDuties.Add(new SoxDutyEntity
        {
            DutyCode = "PO_CREATE",
            DutyName = "Create Purchase Orders",
            ConflictDuties = "[\"PO_APPROVAL\"]",
            IsActive = true
        });

        db.SoxDuties.Add(new SoxDutyEntity
        {
            DutyCode = "PO_APPROVAL",
            DutyName = "Approve Purchase Orders",
            ConflictDuties = "[\"PO_CREATE\"]",
            IsActive = true
        });

        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        await service.AssignDutyAsync(new DutyAssignmentRequest
        {
            UserId = userId,
            DutyType = "PO_CREATE",
            Role = "Purchaser",
            StartDate = DateTime.UtcNow,
            Description = "Create POs",
            AssignedByUserId = adminId
        });

        var conflictResult = await service.AssignDutyAsync(new DutyAssignmentRequest
        {
            UserId = userId,
            DutyType = "PO_APPROVAL",
            Role = "Approver",
            StartDate = DateTime.UtcNow,
            Description = "Approve POs",
            AssignedByUserId = adminId
        });

        Assert.False(conflictResult.Success);
        Assert.Contains("conflicts", conflictResult.Message.ToLower());
    }
}
