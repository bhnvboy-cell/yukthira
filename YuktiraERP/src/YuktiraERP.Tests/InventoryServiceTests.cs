using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class InventoryServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private async Task<MaterialMasterEntity> CreateMaterial(YuktiraDbContext db, string name = "Steel Rod", decimal stock = 100)
    {
        var material = new MaterialMasterEntity { Code = "MAT-001", Name = name, Stock = stock, Price = 10.50m, Status = "Active" };
        db.MaterialMasters.Add(material);
        await db.SaveChangesAsync();
        return material;
    }

    [Fact]
    public async Task CheckAvailabilityAsync_AvailableStock_ReturnsAvailable()
    {
        var db = CreateDb();
        var material = await CreateMaterial(db);
        var service = new InventoryService(db);

        var result = await service.CheckAvailabilityAsync(material.Id, 50, DateTime.UtcNow.AddDays(10));

        Assert.True(result.IsAvailable);
        Assert.Equal(100, result.AvailableQuantity);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_InsufficientStock_ReturnsUnavailable()
    {
        var db = CreateDb();
        var material = await CreateMaterial(db, stock: 10);
        var service = new InventoryService(db);

        var result = await service.CheckAvailabilityAsync(material.Id, 50, DateTime.UtcNow.AddDays(10));

        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_NonExistentMaterial_ReturnsUnavailable()
    {
        var db = CreateDb();
        var service = new InventoryService(db);

        var result = await service.CheckAvailabilityAsync(Guid.NewGuid(), 50, DateTime.UtcNow.AddDays(10));

        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_AtpCalculation()
    {
        var db = CreateDb();
        var material = await CreateMaterial(db, stock: 100);
        var service = new InventoryService(db);

        // Add reservation
        db.Set<StockReservationEntity>().Add(new StockReservationEntity
        {
            MaterialId = material.Id, MaterialName = material.Name,
            Quantity = 20, OrderId = Guid.NewGuid(), Status = "Active", ReservedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await service.CheckAvailabilityAsync(material.Id, 50, DateTime.UtcNow.AddDays(10));

        // ATP = stock(100) - reserved(20) + scheduledReceipts(0) - allocated(0) = 80
        Assert.Equal(80, result.AvailableQuantity);
        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task ReserveStockAsync_SuccessfulReservation()
    {
        var db = CreateDb();
        var material = await CreateMaterial(db, stock: 100);
        var service = new InventoryService(db);
        var orderId = Guid.NewGuid();

        var result = await service.ReserveStockAsync(material.Id, 30, orderId);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.ReservationId);
        Assert.Equal("Stock reserved successfully", result.Message);
    }

    [Fact]
    public async Task ReserveStockAsync_InsufficientStock_ReturnsFailure()
    {
        var db = CreateDb();
        var material = await CreateMaterial(db, stock: 10);
        var service = new InventoryService(db);

        var result = await service.ReserveStockAsync(material.Id, 50, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Contains("Insufficient stock", result.Message);
    }

    [Fact]
    public async Task ReserveStockAsync_NonExistentMaterial_ReturnsFailure()
    {
        var db = CreateDb();
        var service = new InventoryService(db);

        var result = await service.ReserveStockAsync(Guid.NewGuid(), 50, Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("Material not found", result.Message);
    }

    [Fact]
    public async Task ReleaseReservationAsync_ReleasesStock()
    {
        var db = CreateDb();
        var material = await CreateMaterial(db, stock: 100);
        var service = new InventoryService(db);

        var reservation = await service.ReserveStockAsync(material.Id, 40, Guid.NewGuid());
        Assert.True(reservation.Success);

        await service.ReleaseReservationAsync(reservation.ReservationId);

        var available = await service.GetAvailableQuantityAsync(material.Id);
        Assert.Equal(100, available);
    }

    [Fact]
    public async Task ReleaseReservationAsync_NonExistentReservation_DoesNothing()
    {
        var db = CreateDb();
        var service = new InventoryService(db);

        // Should not throw
        await service.ReleaseReservationAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task GetAvailableQuantityAsync_ReturnsCorrectAvailable()
    {
        var db = CreateDb();
        var material = await CreateMaterial(db, stock: 100);
        var service = new InventoryService(db);

        await service.ReserveStockAsync(material.Id, 25, Guid.NewGuid());

        var available = await service.GetAvailableQuantityAsync(material.Id);
        Assert.Equal(75, available);
    }

    [Fact]
    public async Task GetAvailableQuantityAsync_NonExistent_ReturnsZero()
    {
        var db = CreateDb();
        var service = new InventoryService(db);

        var available = await service.GetAvailableQuantityAsync(Guid.NewGuid());
        Assert.Equal(0, available);
    }

    [Fact]
    public async Task GetConfirmedAvailabilityAsync_ReturnsMultiLocationData()
    {
        var db = CreateDb();
        var material = await CreateMaterial(db);
        db.StockItems.Add(new StockItemEntity
        {
            Bin = "WH-A", MaterialName = material.Name, Quantity = 60, UOM = "KG"
        });
        db.StockItems.Add(new StockItemEntity
        {
            Bin = "WH-B", MaterialName = material.Name, Quantity = 40, UOM = "KG"
        });
        await db.SaveChangesAsync();
        var service = new InventoryService(db);

        var result = await service.GetConfirmedAvailabilityAsync(material.Id, "WH-A");

        Assert.Equal(100, result.TotalAvailable);
        Assert.Equal(2, result.Stores.Count);
    }

    [Fact]
    public async Task GetConfirmedAvailabilityAsync_NonExistentMaterial_ReturnsZero()
    {
        var db = CreateDb();
        var service = new InventoryService(db);

        var result = await service.GetConfirmedAvailabilityAsync(Guid.NewGuid(), "WH-A");

        Assert.Equal(0, result.TotalAvailable);
        Assert.Empty(result.Stores);
    }
}
