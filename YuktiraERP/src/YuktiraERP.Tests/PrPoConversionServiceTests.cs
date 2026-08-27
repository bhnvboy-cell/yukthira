using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class PrPoConversionServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private async Task<PurchaseRequisitionEntity> SeedApprovedPrAsync(YuktiraDbContext db, Guid tenantId)
    {
        var pr = new PurchaseRequisitionEntity
        {
            TenantId = tenantId,
            PrNumber = "PR2026000001",
            Date = DateTime.UtcNow,
            Requestor = "Test User",
            Status = "APPROVED",
            DepartmentKey = "PUR",
            TotalAmount = 500m,
            ItemCount = 2
        };
        db.PurchaseRequisitions.Add(pr);
        await db.SaveChangesAsync();

        db.PurchaseRequisitionItems.Add(new PurchaseRequisitionItemEntity
        {
            TenantId = tenantId,
            PurchaseRequisitionId = pr.Id,
            LineNumber = 1,
            MaterialName = "Steel Sheet",
            MaterialCode = "MS-001",
            Quantity = 10,
            UnitPrice = 25m,
            TotalPrice = 250m,
            Status = "OPEN"
        });
        db.PurchaseRequisitionItems.Add(new PurchaseRequisitionItemEntity
        {
            TenantId = tenantId,
            PurchaseRequisitionId = pr.Id,
            LineNumber = 2,
            MaterialName = "Bolts",
            MaterialCode = "MS-002",
            Quantity = 100,
            UnitPrice = 2.5m,
            TotalPrice = 250m,
            Status = "OPEN"
        });
        await db.SaveChangesAsync();
        return pr;
    }

    [Fact]
    public async Task ConvertPrToPoAsync_CreatesPoWithItems()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr = await SeedApprovedPrAsync(db, tenantId);
        var numberRange = new NumberRangeService(db);
        var service = new PrPoConversionService(db, numberRange);

        var request = new ConvertPrToPoRequest { VendorName = "Acme Corp", VendorCode = "V001" };
        var po = await service.ConvertPrToPoAsync(pr.Id, request, "user1");

        Assert.NotEqual(Guid.Empty, po.Id);
        Assert.StartsWith("PO", po.PoNumber);
        Assert.Equal("Acme Corp", po.VendorName);
        Assert.Equal("DRAFT", po.Status);
        Assert.Equal(2, po.ItemCount);
        Assert.Equal(500m, po.TotalAmount);

        var items = await db.PurchaseOrderItems.Where(i => i.PurchaseOrderId == po.Id).ToListAsync();
        Assert.Equal(2, items.Count);

        var updatedPr = await db.PurchaseRequisitions.FindAsync(pr.Id);
        Assert.Equal("CONVERTED", updatedPr!.Status);
        Assert.Equal(po.PoNumber, updatedPr.ConvertedPoNumber);
    }

    [Fact]
    public async Task ConvertPrToPoAsync_NonApprovedPr_Throws()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr = new PurchaseRequisitionEntity
        {
            TenantId = tenantId,
            PrNumber = "PR2026000002",
            Status = "DRAFT"
        };
        db.PurchaseRequisitions.Add(pr);
        await db.SaveChangesAsync();

        var numberRange = new NumberRangeService(db);
        var service = new PrPoConversionService(db, numberRange);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConvertPrToPoAsync(pr.Id, new ConvertPrToPoRequest(), "user1"));
    }

    [Fact]
    public async Task ConvertPrToPoAsync_SelectedItems_OnlyConvertsSelected()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr = await SeedApprovedPrAsync(db, tenantId);
        var numberRange = new NumberRangeService(db);
        var service = new PrPoConversionService(db, numberRange);

        var firstItem = await db.PurchaseRequisitionItems.FirstAsync(i => i.PurchaseRequisitionId == pr.Id);
        var request = new ConvertPrToPoRequest
        {
            VendorName = "Beta Inc",
            SelectedItemIds = new List<Guid> { firstItem.Id }
        };

        var po = await service.ConvertPrToPoAsync(pr.Id, request, "user1");

        Assert.Equal(1, po.ItemCount);
        Assert.Equal(250m, po.TotalAmount);
    }

    [Fact]
    public async Task GetConversionPreviewAsync_ReturnsPreview()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr = await SeedApprovedPrAsync(db, tenantId);
        var numberRange = new NumberRangeService(db);
        var service = new PrPoConversionService(db, numberRange);

        var preview = await service.GetConversionPreviewAsync(pr.Id);

        Assert.Equal(pr.Id, preview.PrId);
        Assert.Equal("PR2026000001", preview.PrNumber);
        Assert.Equal(2, preview.TotalItems);
        Assert.Equal(500m, preview.TotalAmount);
    }

    [Fact]
    public async Task ConvertMultiplePrToPoAsync_ConvertsAll()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var pr1 = await SeedApprovedPrAsync(db, tenantId);

        var pr2 = new PurchaseRequisitionEntity
        {
            TenantId = tenantId,
            PrNumber = "PR2026000003",
            Status = "APPROVED",
            TotalAmount = 100m,
            ItemCount = 1
        };
        db.PurchaseRequisitions.Add(pr2);
        await db.SaveChangesAsync();

        db.PurchaseRequisitionItems.Add(new PurchaseRequisitionItemEntity
        {
            TenantId = tenantId,
            PurchaseRequisitionId = pr2.Id,
            LineNumber = 1,
            MaterialName = "Widget",
            Quantity = 5,
            UnitPrice = 20m,
            TotalPrice = 100m,
            Status = "OPEN"
        });
        await db.SaveChangesAsync();

        var numberRange = new NumberRangeService(db);
        var service = new PrPoConversionService(db, numberRange);

        var results = await service.ConvertMultiplePrToPoAsync(new List<Guid> { pr1.Id, pr2.Id }, "user1");

        Assert.Equal(2, results.Count);
        Assert.All(results, po => Assert.StartsWith("PO", po.PoNumber));
    }
}
