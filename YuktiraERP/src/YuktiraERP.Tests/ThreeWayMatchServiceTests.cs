using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class ThreeWayMatchServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private async Task<(PurchaseOrderEntity po, InvoiceVerificationEntity invoice)> SeedPoAndInvoiceAsync(YuktiraDbContext db, decimal poAmount, decimal invoiceAmount)
    {
        var tenantId = Guid.NewGuid();
        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = "PO2026000200",
            VendorName = "Vendor A",
            TotalAmount = poAmount,
            Status = "APPROVED"
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        db.PurchaseOrderItems.Add(new PurchaseOrderItemEntity
        {
            TenantId = tenantId,
            PurchaseOrderId = po.Id,
            LineNumber = 1,
            MaterialName = "Widget A",
            Quantity = 100,
            UnitPrice = poAmount / 100,
            TotalPrice = poAmount,
            ReceivedQty = 100,
            Status = "Received"
        });
        await db.SaveChangesAsync();

        var invoice = new InvoiceVerificationEntity
        {
            TenantId = tenantId,
            InvoiceNumber = "INV-001",
            PoNumber = "PO2026000200",
            VendorName = "Vendor A",
            Amount = invoiceAmount,
            Date = DateTime.UtcNow,
            Status = "Pending"
        };
        db.InvoiceVerifications.Add(invoice);
        await db.SaveChangesAsync();

        return (po, invoice);
    }

    [Fact]
    public async Task PerformMatchAsync_MatchedAmount_ReturnsMatched()
    {
        var db = CreateDb();
        var (po, invoice) = await SeedPoAndInvoiceAsync(db, 1000m, 1000m);

        var service = new ThreeWayMatchService(db);
        var result = await service.PerformMatchAsync(invoice.Id);

        Assert.True(result.IsMatch);
        Assert.Equal("MATCHED", result.OverallStatus);
        Assert.True(result.PriceWithinTolerance);
    }

    [Fact]
    public async Task PerformMatchAsync_PriceVariance_DetectsMismatch()
    {
        var db = CreateDb();
        var (po, invoice) = await SeedPoAndInvoiceAsync(db, 1000m, 1100m);

        var service = new ThreeWayMatchService(db);
        var result = await service.PerformMatchAsync(invoice.Id);

        Assert.False(result.IsMatch);
        Assert.Equal("PRICE_VARIANCE", result.OverallStatus);
        Assert.Equal(10m, result.PriceVariance);
    }

    [Fact]
    public async Task PerformMatchWithToleranceAsync_WithinTolerance_ReturnsMatch()
    {
        var db = CreateDb();
        var (po, invoice) = await SeedPoAndInvoiceAsync(db, 1000m, 1030m);

        var service = new ThreeWayMatchService(db);
        var result = await service.PerformMatchWithToleranceAsync(invoice.Id, 5m, 5m);

        Assert.True(result.IsMatch);
        Assert.True(result.PriceWithinTolerance);
        Assert.Equal(3m, result.PriceVariance);
    }

    [Fact]
    public async Task PerformMatchWithToleranceAsync_ExceedsTolerance_ReturnsVariance()
    {
        var db = CreateDb();
        var (po, invoice) = await SeedPoAndInvoiceAsync(db, 1000m, 1200m);

        var service = new ThreeWayMatchService(db);
        var result = await service.PerformMatchWithToleranceAsync(invoice.Id, 5m, 5m);

        Assert.False(result.IsMatch);
        Assert.False(result.PriceWithinTolerance);
        Assert.Equal(20m, result.PriceVariance);
    }

    [Fact]
    public async Task PerformMatchAsync_ReturnsLineDetails()
    {
        var db = CreateDb();
        var (po, invoice) = await SeedPoAndInvoiceAsync(db, 1000m, 1000m);

        var service = new ThreeWayMatchService(db);
        var result = await service.PerformMatchAsync(invoice.Id);

        Assert.NotEmpty(result.LineDetails);
        Assert.Equal("Widget A", result.LineDetails[0].MaterialName);
    }
}
