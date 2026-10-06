using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class WorkflowThresholdTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task WF01_CreateThreshold_PersistsStrategyAndOrderedLevels()
    {
        var db = CreateDb();
        var service = new WorkflowThresholdService(db);
        var tenantId = Guid.NewGuid();

        var result = await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest
        {
            DocumentType = "PO",
            Name = "Purchase Order approvals above $50,000",
            MinAmount = 50000m,
            MaxAmount = 150000m,
            Plant = "P1",
            ApproverRole = "MANAGER, ADMIN"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Threshold);
        Assert.Equal("PO", result.Threshold!.DocumentType);
        Assert.Equal(2, result.Threshold.Levels.Count);
        Assert.Equal("MANAGER", result.Threshold.Levels[0].ApproverRole);
        Assert.Equal("ADMIN", result.Threshold.Levels[1].ApproverRole);
        Assert.StartsWith("PO-", result.Threshold.Code);
        Assert.True(result.Threshold.IsActive);

        var stored = await db.ReleaseStrategies.AsNoTracking().SingleAsync(s => s.TenantId == tenantId);
        var codes = await db.ReleaseCodes.AsNoTracking().Where(c => c.ReleaseStrategyId == stored.Id).OrderBy(c => c.Level).ToListAsync();
        Assert.Equal(2, codes.Count);
        Assert.Equal("MANAGER", codes[0].ApproverRole);
        Assert.Equal("ADMIN", codes[1].ApproverRole);
        Assert.All(codes, c => Assert.True(c.IsRequired));
    }

    [Fact]
    public async Task WF02_OverlappingRange_IsRejected()
    {
        var db = CreateDb();
        var service = new WorkflowThresholdService(db);
        var tenantId = Guid.NewGuid();

        var first = await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest
        {
            DocumentType = "PO",
            Name = "Standard PO",
            MinAmount = 0m,
            MaxAmount = 10000m,
            ApproverRole = "MANAGER"
        });
        Assert.True(first.Success);

        var overlapping = await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest
        {
            DocumentType = "PO",
            Name = "Overlapping PO",
            MinAmount = 5000m,
            MaxAmount = 20000m,
            ApproverRole = "ADMIN"
        });

        Assert.False(overlapping.Success);
        Assert.Contains("overlaps", overlapping.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(overlapping.Threshold);
        Assert.Single(await db.ReleaseStrategies.AsNoTracking().Where(s => s.TenantId == tenantId).ToListAsync());

        var reversed = await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest
        {
            DocumentType = "PO",
            Name = "Inverted range",
            MinAmount = 30000m,
            MaxAmount = 20000m,
            ApproverRole = "ADMIN"
        });

        Assert.False(reversed.Success);
        Assert.Contains("greater than", reversed.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WF03_AdjacentRanges_DoNotOverlap()
    {
        var db = CreateDb();
        var service = new WorkflowThresholdService(db);
        var tenantId = Guid.NewGuid();

        var lower = await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest
        {
            DocumentType = "PO",
            MinAmount = 0m,
            MaxAmount = 10000m,
            ApproverRole = "MANAGER"
        });
        var upper = await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest
        {
            DocumentType = "PO",
            MinAmount = 10000m,
            MaxAmount = 50000m,
            ApproverRole = "ADMIN"
        });
        var otherType = await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest
        {
            DocumentType = "PR",
            MinAmount = 0m,
            MaxAmount = 10000m,
            ApproverRole = "ADMIN"
        });

        Assert.True(lower.Success);
        Assert.True(upper.Success);
        Assert.True(otherType.Success);

        var selfUpdate = await service.ValidateNoOverlapAsync(tenantId, "PO", 0m, 10000m, lower.Threshold!.Id);
        Assert.True(selfUpdate.IsValid);

        var otherTenant = await service.ValidateNoOverlapAsync(Guid.NewGuid(), "PO", 0m, 10000m);
        Assert.True(otherTenant.IsValid);
    }

    [Fact]
    public async Task WF04_PreviewAsync_ReturnsMatchingThresholdWithApproverChain()
    {
        var db = CreateDb();
        var service = new WorkflowThresholdService(db);
        var tenantId = Guid.NewGuid();

        await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest
        {
            DocumentType = "PO",
            Name = "High value PO",
            MinAmount = 50000m,
            MaxAmount = 250000m,
            Plant = "P1",
            ApproverRole = "MANAGER, ADMIN"
        });

        var match = await service.PreviewAsync(tenantId, "PO", 75000m, "P1");
        Assert.NotNull(match);
        Assert.Equal("High value PO", match!.Name);
        Assert.Equal(2, match.Levels.Count);
        Assert.Equal(new[] { "MANAGER", "ADMIN" }, match.Levels.Select(l => l.ApproverRole).ToArray());

        Assert.Null(await service.PreviewAsync(tenantId, "PO", 75000m, "P2"));
        Assert.Null(await service.PreviewAsync(tenantId, "PO", 1000000m, "P1"));
        Assert.Null(await service.PreviewAsync(Guid.NewGuid(), "PO", 75000m, "P1"));
        Assert.Null(await service.PreviewAsync(tenantId, "", 75000m, "P1"));
    }

    [Fact]
    public async Task WF05_UpdateThreshold_ReplacesLevelsAndKeepsTenantScope()
    {
        var db = CreateDb();
        var service = new WorkflowThresholdService(db);
        var tenantId = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();

        var created = await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest
        {
            DocumentType = "PO",
            MinAmount = 0m,
            MaxAmount = 10000m,
            ApproverRole = "MANAGER"
        });
        var id = created.Threshold!.Id;

        var crossTenant = await service.UpdateAsync(otherTenant, id, new WorkflowThresholdUpdateRequest
        {
            DocumentType = "PO",
            MinAmount = 0m,
            MaxAmount = 10000m,
            ApproverRole = "ADMIN"
        });

        Assert.False(crossTenant.Success);
        Assert.Equal("Approval threshold not found", crossTenant.Error);

        var updated = await service.UpdateAsync(tenantId, id, new WorkflowThresholdUpdateRequest
        {
            DocumentType = "PO",
            Name = "Widened PO",
            MinAmount = 0m,
            MaxAmount = 40000m,
            IsActive = false,
            Levels = new System.Collections.Generic.List<WorkflowThresholdLevelDto>
            {
                new() { Level = 2, ApproverRole = "ADMIN" },
                new() { Level = 1, ApproverRole = "MANAGER" }
            }
        });

        Assert.True(updated.Success);
        Assert.Equal("Widened PO", updated.Threshold!.Name);
        Assert.False(updated.Threshold.IsActive);
        Assert.Equal(40000m, updated.Threshold.MaxAmount);
        Assert.Equal(new[] { "MANAGER", "ADMIN" }, updated.Threshold.Levels.Select(l => l.ApproverRole).ToArray());

        var listed = await service.ListAsync(tenantId, "PO");
        Assert.Single(listed);

        Assert.True(await service.DeleteAsync(tenantId, id));
        Assert.False(await service.DeleteAsync(tenantId, id));
        Assert.Empty(await service.ListAsync(tenantId));
    }

    [Fact]
    public async Task WF06_ListAsync_FiltersByDocumentType()
    {
        var db = CreateDb();
        var service = new WorkflowThresholdService(db);
        var tenantId = Guid.NewGuid();

        await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest { DocumentType = "PO", MinAmount = 0m, MaxAmount = 1000m, ApproverRole = "MANAGER" });
        await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest { DocumentType = "PO", MinAmount = 1000m, MaxAmount = 2000m, ApproverRole = "ADMIN" });
        await service.CreateAsync(tenantId, new WorkflowThresholdCreateRequest { DocumentType = "PR", MinAmount = 0m, MaxAmount = 1000m, ApproverRole = "ADMIN" });
        await service.CreateAsync(Guid.NewGuid(), new WorkflowThresholdCreateRequest { DocumentType = "PO", MinAmount = 0m, MaxAmount = 1000m, ApproverRole = "ADMIN" });

        var all = await service.ListAsync(tenantId);
        Assert.Equal(3, all.Count);

        var poOnly = await service.ListAsync(tenantId, "PO");
        Assert.Equal(2, poOnly.Count);
        Assert.All(poOnly, t => Assert.Equal("PO", t.DocumentType));
        Assert.Equal(new[] { 0m, 1000m }, poOnly.Select(t => t.MinAmount).ToArray());

        var none = await service.ListAsync(tenantId, "SO");
        Assert.Empty(none);

        var fetched = await service.GetAsync(tenantId, all[0].Id);
        Assert.NotNull(fetched);
        Assert.Null(await service.GetAsync(Guid.NewGuid(), all[0].Id));
    }
}
