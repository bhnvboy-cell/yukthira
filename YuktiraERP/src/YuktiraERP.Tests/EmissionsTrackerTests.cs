using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class EmissionsTrackerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new YuktiraDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static Mock<IAuditService> CreateAuditService(List<AuditEntryDto>? captured = null)
    {
        var mock = new Mock<IAuditService>();
        mock.Setup(a => a.LogAsync(It.IsAny<AuditEntryDto>()))
            .Callback<AuditEntryDto>(entry => { if (captured != null) { captured.Add(entry); } })
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static EmissionsTrackerService CreateService(YuktiraDbContext db, Mock<IAuditService> audit, EmissionsOptions? options = null)
    {
        return new EmissionsTrackerService(db, audit.Object, Options.Create(options ?? new EmissionsOptions()));
    }

    private static async Task SeedFactorsAsync(YuktiraDbContext db)
    {
        db.EmissionFactors.Add(new EmissionFactorEntity
        {
            TenantId = Guid.Empty, Scope = 1, SourceType = "Process", MaterialCode = "",
            Unit = "kg", KgCo2ePerUnit = 0.85m, Region = "DEFAULT", IsActive = true
        });
        db.EmissionFactors.Add(new EmissionFactorEntity
        {
            TenantId = Guid.Empty, Scope = 2, SourceType = "Energy", MaterialCode = "",
            Unit = "kWh", KgCo2ePerUnit = 0.42m, Region = "DEFAULT", IsActive = true
        });
        db.EmissionFactors.Add(new EmissionFactorEntity
        {
            TenantId = Guid.Empty, Scope = 3, SourceType = "Freight", MaterialCode = "",
            Unit = "ton-km", KgCo2ePerUnit = 0.062m, Region = "DEFAULT", IsActive = true
        });
        db.EmissionFactors.Add(new EmissionFactorEntity
        {
            TenantId = Guid.Empty, Scope = 3, SourceType = "Material", MaterialCode = "",
            Unit = "kg", KgCo2ePerUnit = 1.2m, Region = "DEFAULT", IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedOperationalDataAsync(YuktiraDbContext db)
    {
        db.ProductionOrders.Add(new ProductionOrderEntity
        {
            TenantId = TenantId,
            OrderNumber = "PRO-EM-1",
            ProductName = "Starch",
            Quantity = 1000m,
            Status = "COMPLETED",
            YieldQty = 1000m,
            ScrapQty = 0m,
            ConfirmedAt = DateTime.UtcNow
        });

        var header = new MaterialDocumentHeaderEntity
        {
            TenantId = TenantId.ToString(),
            DocumentNumber = "MB-EM-1",
            MovementType = 101,
            PostingDate = DateTime.UtcNow,
            Status = "Posted"
        };
        db.MaterialDocumentHeaders.Add(header);
        await db.SaveChangesAsync();

        db.MaterialDocumentItems.Add(new MaterialDocumentItemEntity
        {
            TenantId = TenantId.ToString(),
            MaterialDocumentHeaderId = header.Id.ToString(),
            MovementType = 101,
            Quantity = 50m,
            MaterialCode = "RM-CORN",
            MaterialName = "Corn",
            UnitOfMeasure = "kg"
        });
        db.HandlingUnits.Add(new HandlingUnitEntity
        {
            TenantId = TenantId,
            HUNumber = "HU-EM-1",
            MaterialCode = "FG-STARCH",
            NetWeight = 2000m,
            GrossWeight = 2100m,
            PackedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task EM01_Recompute_Scope1AndScope3_SummariesMatch()
    {
        using var db = CreateDb();
        await SeedFactorsAsync(db);
        await SeedOperationalDataAsync(db);
        var period = DateTime.UtcNow.ToString("yyyy-MM");

        var service = CreateService(db, CreateAuditService());
        var recompute = await service.RecomputeAsync(TenantId, "tester", CancellationToken.None);

        Assert.True(recompute.Enabled);
        Assert.Equal(1, recompute.Scope1Count);
        Assert.Equal(0, recompute.Scope2Count);
        Assert.Equal(2, recompute.Scope3Count);
        Assert.Equal(3, recompute.Inserted);
        Assert.True(recompute.SkippedSources >= 1);

        var summary = await service.GetSummaryAsync(TenantId, period, CancellationToken.None);

        Assert.Equal(period, summary.Period);
        Assert.Equal(850m, summary.Scope1Kg);
        Assert.Equal(0m, summary.Scope2Kg);
        Assert.Equal(72.4m, summary.Scope3Kg);
        Assert.Equal(922.4m, summary.TotalKg);
        Assert.Equal(1000m, summary.OutputQuantity);
        Assert.Equal(0.9224m, summary.PerUnitKg);
        Assert.Equal("kg", summary.Unit);
        Assert.Equal(3, summary.BySourceType.Count);
        Assert.Contains(summary.BySourceType, s => s.SourceType == "Process" && s.Kg == 850m);
        Assert.Contains(summary.BySourceType, s => s.SourceType == "Material" && s.Kg == 60m);
        Assert.Contains(summary.BySourceType, s => s.SourceType == "Freight" && s.Kg == 12.4m);
    }

    [Fact]
    public async Task EM02_MissingFactor_SkipsSources_WithoutThrowing()
    {
        using var db = CreateDb();
        db.EmissionFactors.Add(new EmissionFactorEntity
        {
            TenantId = Guid.Empty, Scope = 1, SourceType = "Process", Unit = "kg",
            KgCo2ePerUnit = 0.85m, IsActive = true
        });
        db.ProductionOrders.Add(new ProductionOrderEntity
        {
            TenantId = TenantId,
            OrderNumber = "PRO-EM-2",
            ProductName = "Starch",
            Quantity = 1000m,
            Status = "COMPLETED",
            YieldQty = 1000m,
            ConfirmedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateAuditService());
        var recompute = await service.RecomputeAsync(TenantId, "tester", CancellationToken.None);

        Assert.Equal(1, recompute.Inserted);
        Assert.Equal(1, recompute.Scope1Count);
        Assert.Equal(0, recompute.Scope2Count);
        Assert.Equal(0, recompute.Scope3Count);
        Assert.True(recompute.SkippedSources >= 2);
        Assert.NotEmpty(recompute.Skipped);

        var summary = await service.GetSummaryAsync(TenantId, null, CancellationToken.None);
        Assert.Equal(850m, summary.Scope1Kg);
        Assert.Equal(0m, summary.Scope2Kg);
        Assert.Equal(0m, summary.Scope3Kg);
        Assert.Equal(850m, summary.TotalKg);
    }

    [Fact]
    public async Task EM03_UpsertFactor_AuditsAndFiltersByScope()
    {
        using var db = CreateDb();
        var auditEntries = new List<AuditEntryDto>();
        var audit = CreateAuditService(auditEntries);
        var service = CreateService(db, audit);

        var created = await service.UpsertFactorAsync(new EmissionFactorDto
        {
            Scope = 3,
            SourceType = "Material",
            MaterialCode = "RM-CORN",
            Unit = "kg",
            KgCo2ePerUnit = 1.5m,
            Region = "EU",
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow
        }, TenantId, TenantId.ToString(), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(TenantId, created.TenantId);
        Assert.Equal(1.5m, created.KgCo2ePerUnit);
        Assert.Single(auditEntries);
        Assert.Equal("EmissionFactor", auditEntries[0].EntityName);
        Assert.Equal(ActionType.Create, auditEntries[0].ActionType);
        Assert.Equal(TenantId, auditEntries[0].TenantId);

        var updated = await service.UpsertFactorAsync(new EmissionFactorDto
        {
            Id = created.Id,
            Scope = 3,
            SourceType = "Material",
            MaterialCode = "RM-CORN",
            Unit = "kg",
            KgCo2ePerUnit = 1.75m,
            Region = "EU",
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow
        }, TenantId, TenantId.ToString(), CancellationToken.None);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(1.75m, updated.KgCo2ePerUnit);
        Assert.Equal(2, auditEntries.Count);
        Assert.Equal(ActionType.Update, auditEntries[1].ActionType);

        db.EmissionFactors.Add(new EmissionFactorEntity
        {
            TenantId = Guid.Empty, Scope = 3, SourceType = "Freight", Unit = "ton-km",
            KgCo2ePerUnit = 0.062m, IsActive = true
        });
        db.EmissionFactors.Add(new EmissionFactorEntity
        {
            TenantId = Guid.Empty, Scope = 1, SourceType = "Process", Unit = "kg",
            KgCo2ePerUnit = 0.85m, IsActive = true
        });
        await db.SaveChangesAsync();

        var scopeThree = await service.GetFactorsAsync(TenantId, 3, null, CancellationToken.None);
        Assert.NotEmpty(scopeThree);
        Assert.All(scopeThree, factor => Assert.Equal(3, factor.Scope));
        Assert.Contains(scopeThree, factor => factor.SourceType == "Material" && factor.KgCo2ePerUnit == 1.75m);
        Assert.Contains(scopeThree, factor => factor.SourceType == "Freight" && factor.TenantId == Guid.Empty);

        var processOnly = await service.GetFactorsAsync(TenantId, null, "Process", CancellationToken.None);
        Assert.NotEmpty(processOnly);
        Assert.All(processOnly, factor => Assert.Equal("Process", factor.SourceType));
    }

    [Fact]
    public async Task EM03_SeedFactorOverride_InsertsTenantRowWithoutPrimaryKeyClash()
    {
        using var db = CreateDb();
        var seedId = Guid.NewGuid();
        db.EmissionFactors.Add(new EmissionFactorEntity
        {
            Id = seedId, TenantId = Guid.Empty, Scope = 2, SourceType = "Energy",
            Unit = "kWh", KgCo2ePerUnit = 0.42m, IsActive = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, CreateAuditService());
        var overridden = await service.UpsertFactorAsync(new EmissionFactorDto
        {
            Id = seedId,
            Scope = 2,
            SourceType = "Energy",
            Unit = "kWh",
            KgCo2ePerUnit = 0.55m,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow
        }, TenantId, TenantId.ToString(), CancellationToken.None);

        Assert.NotEqual(seedId, overridden.Id);
        Assert.Equal(TenantId, overridden.TenantId);
        Assert.Equal(0.55m, overridden.KgCo2ePerUnit);
        Assert.Equal(2, await db.EmissionFactors.AsNoTracking().CountAsync(f => f.SourceType == "Energy"));

        var energy = await service.GetFactorsAsync(TenantId, 2, "Energy", CancellationToken.None);
        Assert.Equal(2, energy.Count);
    }

    [Fact]
    public async Task EM04_Recompute_Twice_IsIdempotent()
    {
        using var db = CreateDb();
        await SeedFactorsAsync(db);
        await SeedOperationalDataAsync(db);

        var service = CreateService(db, CreateAuditService());
        var first = await service.RecomputeAsync(TenantId, "tester", CancellationToken.None);
        var second = await service.RecomputeAsync(TenantId, "tester", CancellationToken.None);

        Assert.Equal(first.Inserted, second.Inserted);
        Assert.Equal(3, second.Inserted);
        Assert.Equal(second.Scope1Count, first.Scope1Count);
        Assert.Equal(second.Scope3Count, first.Scope3Count);
        Assert.Equal(3, await db.EmissionLogs.AsNoTracking().CountAsync(l => l.TenantId == TenantId));
    }

    [Fact]
    public async Task EM05_LedgerFiltersByScopeAndPaginates()
    {
        using var db = CreateDb();
        await SeedFactorsAsync(db);
        await SeedOperationalDataAsync(db);

        var service = CreateService(db, CreateAuditService());
        await service.RecomputeAsync(TenantId, "tester", CancellationToken.None);

        var all = await service.GetLedgerAsync(TenantId, null, null, 1, 10, CancellationToken.None);
        Assert.Equal(3, all.TotalCount);
        Assert.Equal(3, all.Items.Count);

        var scopeThree = await service.GetLedgerAsync(TenantId, null, 3, 1, 10, CancellationToken.None);
        Assert.Equal(2, scopeThree.TotalCount);
        Assert.All(scopeThree.Items, item => Assert.Equal(3, item.Scope));

        var paged = await service.GetLedgerAsync(TenantId, null, null, 1, 2, CancellationToken.None);
        Assert.Equal(3, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(1, paged.Page);
        Assert.Equal(2, paged.PageSize);

        var secondPage = await service.GetLedgerAsync(TenantId, null, null, 2, 2, CancellationToken.None);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Single(secondPage.Items);

        var otherTenant = await service.GetLedgerAsync(Guid.NewGuid(), null, null, 1, 10, CancellationToken.None);
        Assert.Equal(0, otherTenant.TotalCount);
        Assert.Empty(otherTenant.Items);
    }

    [Fact]
    public async Task EM06_DisabledTracker_WritesNothing()
    {
        using var db = CreateDb();
        await SeedFactorsAsync(db);
        await SeedOperationalDataAsync(db);

        var service = CreateService(db, CreateAuditService(), new EmissionsOptions { Enabled = false });
        var recompute = await service.RecomputeAsync(TenantId, "tester", CancellationToken.None);

        Assert.False(recompute.Enabled);
        Assert.Equal(0, recompute.Inserted);
        Assert.Empty(recompute.Skipped);
        Assert.Equal(0, await db.EmissionLogs.AsNoTracking().CountAsync());

        var summary = await service.GetSummaryAsync(TenantId, null, CancellationToken.None);
        Assert.Equal(0m, summary.TotalKg);
        Assert.Empty(summary.BySourceType);
    }
}
