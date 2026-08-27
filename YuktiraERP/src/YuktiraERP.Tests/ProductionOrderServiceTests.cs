using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Tests;

public class ProductionOrderServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private async Task<ProductionOrderEntity> SeedOrderAsync(YuktiraDbContext db, string status = "PLANNED")
    {
        var tenantId = Guid.NewGuid();
        var order = new ProductionOrderEntity
        {
            TenantId = tenantId,
            OrderNumber = $"PO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6]}",
            ProductName = "Test Product",
            Quantity = 100,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            Status = status
        };
        db.ProductionOrders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task SeedOrder_SetsCorrectDefaults()
    {
        var db = CreateDb();
        var order = await SeedOrderAsync(db);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal("PLANNED", order.Status);
        Assert.Equal("Test Product", order.ProductName);
        Assert.Equal(100, order.Quantity);
    }

    [Fact]
    public async Task ProductionOrder_HasCorrectDates()
    {
        var db = CreateDb();
        var start = DateTime.UtcNow;
        var end = start.AddDays(14);

        var order = new ProductionOrderEntity
        {
            TenantId = Guid.NewGuid(),
            OrderNumber = "PO-001",
            ProductName = "Widget",
            Quantity = 50,
            StartDate = start,
            EndDate = end,
            Status = "PLANNED"
        };
        db.ProductionOrders.Add(order);
        await db.SaveChangesAsync();

        Assert.Equal(start.Date, order.StartDate.Date);
        Assert.Equal(end.Date, order.EndDate.Date);
        Assert.True(order.EndDate > order.StartDate);
    }

    [Fact]
    public async Task ProductionOrder_CanStorePlannedAndActualCosts()
    {
        var db = CreateDb();
        var order = await SeedOrderAsync(db);
        order.PlannedCost = 10000m;
        order.ActualCost = 9500m;
        await db.SaveChangesAsync();

        var refreshed = await db.ProductionOrders.FindAsync(order.Id);
        Assert.Equal(10000m, refreshed!.PlannedCost);
        Assert.Equal(9500m, refreshed!.ActualCost);
    }

    [Fact]
    public async Task ProductionOrder_YieldAndScrapTracking()
    {
        var db = CreateDb();
        var order = await SeedOrderAsync(db, "COMPLETED");
        order.YieldQty = 95;
        order.ScrapQty = 5;
        await db.SaveChangesAsync();

        var refreshed = await db.ProductionOrders.FindAsync(order.Id);
        Assert.Equal(95, refreshed!.YieldQty);
        Assert.Equal(5, refreshed.ScrapQty);
        Assert.Equal(100, refreshed.YieldQty + refreshed.ScrapQty);
    }

    [Fact]
    public async Task ProductionOrder_StatusTransitions()
    {
        var db = CreateDb();
        var order = await SeedOrderAsync(db);

        var validStatuses = new[] { "PLANNED", "RELEASED", "IN_PROGRESS", "COMPLETED", "TECO", "CANCELLED" };
        foreach (var status in validStatuses)
        {
            order.Status = status;
            await db.SaveChangesAsync();

            var refreshed = await db.ProductionOrders.FindAsync(order.Id);
            Assert.Equal(status, refreshed!.Status);
        }
    }

    [Fact]
    public async Task ProductionOrder_ReleaseTracking()
    {
        var db = CreateDb();
        var order = await SeedOrderAsync(db);
        var userId = "user-release-1";
        var releaseTime = DateTime.UtcNow;

        order.Status = "RELEASED";
        order.ReleasedAt = releaseTime;
        order.ReleaseBy = userId;
        await db.SaveChangesAsync();

        var refreshed = await db.ProductionOrders.FindAsync(order.Id);
        Assert.Equal("RELEASED", refreshed!.Status);
        Assert.NotNull(refreshed.ReleasedAt);
        Assert.Equal(userId, refreshed.ReleaseBy);
    }

    [Fact]
    public async Task ProductionOrder_ConfirmTracking()
    {
        var db = CreateDb();
        var order = await SeedOrderAsync(db, "IN_PROGRESS");
        var userId = "user-confirm-1";

        order.Status = "COMPLETED";
        order.ConfirmedAt = DateTime.UtcNow;
        order.ConfirmBy = userId;
        order.YieldQty = 98;
        order.ScrapQty = 2;
        await db.SaveChangesAsync();

        var refreshed = await db.ProductionOrders.FindAsync(order.Id);
        Assert.Equal("COMPLETED", refreshed!.Status);
        Assert.NotNull(refreshed.ConfirmedAt);
        Assert.Equal(userId, refreshed.ConfirmBy);
        Assert.Equal(98, refreshed.YieldQty);
        Assert.Equal(2, refreshed.ScrapQty);
    }

    [Fact]
    public async Task ProductionOrder_TecoTracking()
    {
        var db = CreateDb();
        var order = await SeedOrderAsync(db, "COMPLETED");

        order.Status = "TECO";
        order.TecodAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var refreshed = await db.ProductionOrders.FindAsync(order.Id);
        Assert.Equal("TECO", refreshed!.Status);
        Assert.NotNull(refreshed.TecodAt);
    }

    [Fact]
    public async Task ProductionOrder_CancelTracking()
    {
        var db = CreateDb();
        var order = await SeedOrderAsync(db);

        order.Status = "CANCELLED";
        order.CancelledAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var refreshed = await db.ProductionOrders.FindAsync(order.Id);
        Assert.Equal("CANCELLED", refreshed!.Status);
        Assert.NotNull(refreshed.CancelledAt);
    }

    [Fact]
    public async Task ProductionOrder_BatchNumberTracking()
    {
        var db = CreateDb();
        var order = await SeedOrderAsync(db);
        order.BatchNo = "BATCH-2026-001";
        await db.SaveChangesAsync();

        var refreshed = await db.ProductionOrders.FindAsync(order.Id);
        Assert.Equal("BATCH-2026-001", refreshed!.BatchNo);
    }

    [Fact]
    public async Task ProductionOrder_MultipleOrdersPerTenant()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            db.ProductionOrders.Add(new ProductionOrderEntity
            {
                TenantId = tenantId,
                OrderNumber = $"ORD-{i:D3}",
                ProductName = $"Product {i}",
                Quantity = (i + 1) * 10,
                Status = "PLANNED"
            });
        }
        await db.SaveChangesAsync();

        var orders = await db.ProductionOrders.Where(o => o.TenantId == tenantId).ToListAsync();
        Assert.Equal(5, orders.Count);
        Assert.All(orders, o => Assert.Equal(tenantId, o.TenantId));
    }
}
