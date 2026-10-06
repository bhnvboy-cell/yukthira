using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Core.Security;

namespace YuktiraERP.Tests;

public class SecurityWorkbenchTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public void SEC01_SecurityRoleRank_Behavior()
    {
        Assert.True(SecurityRoleRank.Meets("SUPER_USER", "ADMIN"));
        Assert.True(SecurityRoleRank.Meets("ADMIN", "NORMAL_USER"));
        Assert.False(SecurityRoleRank.Meets("NORMAL_USER", "ADMIN"));
        Assert.False(SecurityRoleRank.Meets("READ_ONLY", "NORMAL_USER"));
        Assert.False(SecurityRoleRank.Meets(null, "NORMAL_USER"));
        Assert.False(SecurityRoleRank.Meets("READ_ONLY", ""));
        Assert.Equal(60, SecurityRoleRank.GetRank("POWER_USER"));
        Assert.True(SecurityRoleRank.IsSuperUser("super_user"));
    }

    [Fact]
    public async Task SEC02_AuthorizationTrace_QueryPurge()
    {
        var db = CreateDb();
        var service = new AuthorizationTraceService(db);
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        await service.TraceAsync(new AuthorizationTraceEntry
        {
            TenantId = tenantId,
            UserName = "older_user",
            Role = "ADMIN",
            Decision = "Allow",
            ResourceType = "Page",
            Resource = "Admin/Index",
            RuleSource = "PageAccess",
            Reason = "granted"
        });
        await Task.Delay(50);
        await service.TraceAsync(new AuthorizationTraceEntry
        {
            TenantId = tenantId,
            UserName = "newer_user",
            Role = "READ_ONLY",
            Decision = "Deny",
            ResourceType = "TCode",
            Resource = "F-200",
            RuleSource = "TCodeAuthCheck",
            Reason = "denied"
        });
        await service.TraceAsync(new AuthorizationTraceEntry
        {
            TenantId = otherTenantId,
            UserName = "other_user",
            Decision = "Allow"
        });

        var older = await db.AuthorizationTraces.SingleAsync(t => t.UserName == "older_user");
        var newer = await db.AuthorizationTraces.SingleAsync(t => t.UserName == "newer_user");

        var result = await service.QueryAsync(new AuthorizationTraceQuery(), tenantId);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(newer.Id, result.Entries[0].Id);
        Assert.Equal(older.Id, result.Entries[1].Id);
        Assert.True(result.Entries[0].CreatedAt > result.Entries[1].CreatedAt);
        Assert.Equal("newer_user", result.Entries[0].UserName);
        Assert.Equal("Deny", result.Entries[0].Decision);

        var allTenants = await service.QueryAsync(new AuthorizationTraceQuery(), null);
        Assert.Equal(3, allTenants.TotalCount);

        var purged = await service.PurgeAsync(tenantId, newer.CreatedAt);
        Assert.Equal(1, purged);

        var remaining = await db.AuthorizationTraces.Where(t => t.TenantId == tenantId).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(newer.Id, remaining[0].Id);
        Assert.Equal(1, await db.AuthorizationTraces.CountAsync(t => t.TenantId == otherTenantId));
    }

    [Fact]
    public async Task SEC03_RunSoDScan_DetectsConflictOnce()
    {
        var db = CreateDb();
        var service = new SoxComplianceService(db);
        var tenantId = Guid.NewGuid();

        var user = new AdminUserEntity
        {
            UserId = "alice01",
            UserName = "alice",
            Role = "NORMAL_USER",
            IsActive = true
        };
        db.AdminUsers.Add(user);
        db.SoxDuties.Add(new SoxDutyEntity
        {
            TenantId = tenantId,
            DutyCode = "DUTY_A",
            DutyName = "Duty A",
            ConflictDuties = "[\"DUTY_B\"]",
            ActionType = "CREATE",
            IsActive = true
        });
        db.SoxDuties.Add(new SoxDutyEntity
        {
            TenantId = tenantId,
            DutyCode = "DUTY_B",
            DutyName = "Duty B",
            ConflictDuties = "[\"DUTY_A\"]",
            ActionType = "APPROVE",
            IsActive = true
        });
        await db.SaveChangesAsync();

        db.SoxAssignments.Add(new SoxAssignmentEntity
        {
            TenantId = tenantId,
            UserId = user.Id.ToString(),
            UserName = "alice",
            DutyCode = "DUTY_A",
            IsActive = true
        });
        db.SoxAssignments.Add(new SoxAssignmentEntity
        {
            TenantId = tenantId,
            UserId = user.Id.ToString(),
            UserName = "alice",
            DutyCode = "DUTY_B",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var scan = await service.RunSoDScanAsync(tenantId, "tester");
        Assert.Equal(1, scan.UsersScanned);
        Assert.Equal(2, scan.DutiesEvaluated);
        Assert.Equal(1, scan.NewViolations);
        Assert.Equal(0, scan.ExistingSkipped);
        Assert.Single(scan.Violations);
        Assert.Equal("Open", scan.Violations[0].Status);
        Assert.Contains("DUTY_A", scan.Violations[0].Description);
        Assert.Contains("DUTY_B", scan.Violations[0].Description);

        var violation = await db.SoxViolations.SingleAsync(v => v.TenantId == tenantId);
        Assert.Equal("Open", violation.Status);
        Assert.Equal("SoD", violation.ViolationType);
        Assert.Equal(user.Id.ToString(), violation.UserId);
        Assert.Equal("High", violation.Severity);
        Assert.Equal("DUTY_A", violation.DutyCode1);
        Assert.Equal("DUTY_B", violation.DutyCode2);

        var scan2 = await service.RunSoDScanAsync(tenantId, "tester");
        Assert.Equal(0, scan2.NewViolations);
        Assert.Equal(1, scan2.ExistingSkipped);
        Assert.Equal(1, await db.SoxViolations.CountAsync(v => v.TenantId == tenantId));
    }

    [Fact]
    public async Task SEC04_RunSoDScan_DerivesDutyFromTCodeAccess()
    {
        var db = CreateDb();
        var service = new SoxComplianceService(db);
        var tenantId = Guid.NewGuid();

        var user = new AdminUserEntity
        {
            UserId = "bob01",
            UserName = "bob",
            Role = "NORMAL_USER",
            IsActive = true
        };
        db.AdminUsers.Add(user);
        db.TransactionCodes.Add(new TransactionCodeEntity
        {
            Code = "TEST1",
            Name = "Test Transaction",
            Module = "FI",
            Status = "Active",
            RequiredRole = "NORMAL_USER"
        });
        db.SoxDuties.Add(new SoxDutyEntity
        {
            TenantId = tenantId,
            DutyCode = "DUTY_T1",
            DutyName = "TCode Duty",
            TransactionCode = "TEST1",
            ConflictDuties = "[\"DUTY_T2\"]",
            IsActive = true
        });
        db.SoxDuties.Add(new SoxDutyEntity
        {
            TenantId = tenantId,
            DutyCode = "DUTY_T2",
            DutyName = "Explicit Duty",
            ConflictDuties = "[\"DUTY_T1\"]",
            IsActive = true
        });
        await db.SaveChangesAsync();

        db.SoxAssignments.Add(new SoxAssignmentEntity
        {
            TenantId = tenantId,
            UserId = user.Id.ToString(),
            UserName = "bob",
            DutyCode = "DUTY_T2",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var scan = await service.RunSoDScanAsync(tenantId, "tester");
        Assert.Equal(1, scan.NewViolations);
        Assert.Equal(2, scan.DutiesEvaluated);

        var violation = await db.SoxViolations.SingleAsync(v => v.TenantId == tenantId);
        Assert.Equal("Open", violation.Status);
        Assert.Equal("TEST1", violation.TransactionCode);
        Assert.Contains("DUTY_T1", violation.Description);
        Assert.Contains("DUTY_T2", violation.Description);
    }

    [Fact]
    public async Task SEC05_GetEffectiveDuties_ExplicitAndDerived()
    {
        var db = CreateDb();
        var service = new SoxComplianceService(db);
        var tenantId = Guid.NewGuid();

        var user = new AdminUserEntity
        {
            UserId = "carol01",
            UserName = "carol",
            Role = "NORMAL_USER",
            IsActive = true
        };
        db.AdminUsers.Add(user);
        db.TransactionCodes.Add(new TransactionCodeEntity
        {
            Code = "TEST1",
            Name = "Test Transaction",
            Module = "FI",
            Status = "Active",
            RequiredRole = "NORMAL_USER"
        });
        db.SoxDuties.Add(new SoxDutyEntity
        {
            TenantId = tenantId,
            DutyCode = "DUTY_T1",
            DutyName = "TCode Duty",
            TransactionCode = "TEST1",
            IsActive = true
        });
        db.SoxDuties.Add(new SoxDutyEntity
        {
            TenantId = tenantId,
            DutyCode = "DUTY_T2",
            DutyName = "Explicit Duty",
            IsActive = true
        });
        await db.SaveChangesAsync();

        db.SoxAssignments.Add(new SoxAssignmentEntity
        {
            TenantId = tenantId,
            UserId = user.Id.ToString(),
            UserName = "carol",
            DutyCode = "DUTY_T2",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var duties = await service.GetEffectiveDutiesAsync(tenantId, user.Id.ToString());
        Assert.Equal(2, duties.Count);

        var explicitDuty = duties.Single(d => d.DutyCode == "DUTY_T2");
        Assert.Equal("Explicit", explicitDuty.Source);
        Assert.Equal("carol", explicitDuty.UserName);
        Assert.Equal("Explicit Duty", explicitDuty.DutyName);

        var derivedDuty = duties.Single(d => d.DutyCode == "DUTY_T1");
        Assert.Equal("TCode", derivedDuty.Source);
        Assert.Equal(user.Id.ToString(), derivedDuty.UserId);
        Assert.Equal("carol", derivedDuty.UserName);
        Assert.Equal("NORMAL_USER", derivedDuty.Role);
        Assert.Equal("derived", derivedDuty.AssignedBy);
    }
}
