using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class Nl2SqlTests
{
    private static YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new YuktiraDbContext(options);
    }

    private static Nl2SqlService CreateService(YuktiraDbContext db, Nl2SqlOptions? options = null)
    {
        return new Nl2SqlService(
            db,
            new ZqmNonConformanceService(db),
            new AuditService(db),
            Options.Create(options ?? new Nl2SqlOptions()));
    }

    private static PurchaseOrderEntity PurchaseOrder(Guid tenantId, string poNumber, decimal amount, string status, DateTime date)
    {
        return new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = poNumber,
            VendorCode = "V-100",
            VendorName = "Acme Supplies",
            ItemName = "Component",
            Amount = amount,
            TotalAmount = amount,
            Status = status,
            Date = date
        };
    }

    private static InspectionLotEntity InspectionLot(Guid tenantId, string lotNumber, string status, string materialCode)
    {
        return new InspectionLotEntity
        {
            TenantId = tenantId,
            LotNumber = lotNumber,
            MaterialCode = materialCode,
            MaterialName = "Material",
            Plant = "1000",
            Quantity = "10",
            Status = status
        };
    }

    private static InspectionResultEntity InspectionResult(Guid tenantId, string evaluation)
    {
        return new InspectionResultEntity
        {
            TenantId = tenantId,
            ResultId = Guid.NewGuid().ToString("N"),
            LotNumber = $"LOT-{Guid.NewGuid():N}".Substring(0, 12),
            Characteristic = "DIMENSION",
            Evaluation = evaluation,
            Status = evaluation == "Fail" ? "Failed" : "Passed"
        };
    }

    [Fact]
    public async Task NL01_RejectsInjectionAndUnionAttempts()
    {
        await using var db = CreateInMemoryDb();
        var service = CreateService(db);
        var tenantId = Guid.NewGuid();

        var injection = await service.QueryAsync(new Nl2SqlQueryRequest
        {
            TenantId = tenantId,
            Text = "show purchase orders; drop table users"
        });
        Assert.False(injection.Success);
        Assert.NotNull(injection.Error);
        Assert.Contains("disallowed", injection.Error);

        var union = await service.QueryAsync(new Nl2SqlQueryRequest
        {
            TenantId = tenantId,
            Text = "show purchase orders union select password from users"
        });
        Assert.False(union.Success);
        Assert.NotNull(union.Error);
        Assert.Contains("disallowed", union.Error);
    }

    [Fact]
    public async Task NL02_ShowPurchaseOrders_HonorsLimitAndReturnsColumns()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.PurchaseOrders.AddRange(
            PurchaseOrder(tenantId, "PO-1", 100m, "Pending", DateTime.UtcNow.AddDays(-2)),
            PurchaseOrder(tenantId, "PO-2", 200m, "Released", DateTime.UtcNow.AddDays(-1)),
            PurchaseOrder(tenantId, "PO-3", 300m, "Released", DateTime.UtcNow));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.QueryAsync(new Nl2SqlQueryRequest
        {
            TenantId = tenantId,
            Text = "show 2 purchase orders"
        });

        Assert.True(result.Success, result.Error);
        Assert.Equal("show", result.Intent);
        Assert.Equal("PurchaseOrders", result.Entity);
        Assert.Equal(2, result.RowCount);
        Assert.Contains("PoNumber", result.Columns);
        Assert.Contains("LIMIT 2", result.Sql);
    }

    [Fact]
    public async Task NL03_CountInspectionLots_IsScopedToTenant()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        db.InspectionLots.AddRange(
            InspectionLot(tenantId, "LOT-1", "Created", "M-1"),
            InspectionLot(tenantId, "LOT-2", "Failed", "M-1"),
            InspectionLot(otherTenantId, "LOT-3", "Created", "M-1"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.QueryAsync(new Nl2SqlQueryRequest
        {
            TenantId = tenantId,
            Text = "count inspection lots"
        });

        Assert.True(result.Success, result.Error);
        Assert.Equal("count", result.Intent);
        var row = Assert.Single(result.Rows);
        Assert.Equal(2, Assert.IsType<int>(row["Count"]));
    }

    [Fact]
    public async Task NL04_GroupByStatus_AggregatesInspectionLots()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.InspectionLots.AddRange(
            InspectionLot(tenantId, "LOT-1", "Created", "M-1"),
            InspectionLot(tenantId, "LOT-2", "Created", "M-1"),
            InspectionLot(tenantId, "LOT-3", "Failed", "M-1"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.QueryAsync(new Nl2SqlQueryRequest
        {
            TenantId = tenantId,
            Text = "group by status of inspection lots"
        });

        Assert.True(result.Success, result.Error);
        Assert.Equal("groupby", result.Intent);
        Assert.Equal(2, result.Rows.Count);
        var created = result.Rows.Single(r => Equals(r["Status"], "Created"));
        Assert.Equal(2, Assert.IsType<int>(created["Count"]));
        var failed = result.Rows.Single(r => Equals(r["Status"], "Failed"));
        Assert.Equal(1, Assert.IsType<int>(failed["Count"]));
    }

    [Fact]
    public async Task NL05_DraftsNonConformance_WhenDefectRateExceedsThreshold()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.InspectionResults.AddRange(
            InspectionResult(tenantId, "Fail"),
            InspectionResult(tenantId, "Fail"),
            InspectionResult(tenantId, "Fail"),
            InspectionResult(tenantId, "Pass"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.DraftAsync(new Nl2SqlDraftRequest
        {
            TenantId = tenantId,
            UserId = Guid.NewGuid().ToString(),
            Text = "draft non-conformance ticket if defect rate > 20"
        });

        Assert.True(result.Success, result.Error);
        Assert.True(result.Drafted);
        Assert.Equal(75m, result.Value);
        Assert.Equal(20m, result.Threshold);
        Assert.False(string.IsNullOrEmpty(result.NonConformanceId));
        var nc = Assert.Single(db.NonConformances);
        Assert.Equal("NL2SQL", nc.DetectedBy);
        Assert.Contains("75", nc.DefectDescription);
    }

    [Fact]
    public async Task NL06_DoesNotDraft_WhenMetricIsBelowThreshold()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.InspectionResults.AddRange(
            InspectionResult(tenantId, "Fail"),
            InspectionResult(tenantId, "Pass"),
            InspectionResult(tenantId, "Pass"),
            InspectionResult(tenantId, "Pass"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.DraftAsync(new Nl2SqlDraftRequest
        {
            TenantId = tenantId,
            Text = "draft non-conformance ticket if defect rate > 90"
        });

        Assert.True(result.Success, result.Error);
        Assert.False(result.Drafted);
        Assert.Equal(25m, result.Value);
        Assert.Null(result.NonConformanceId);
        Assert.Empty(db.NonConformances);
    }
}
