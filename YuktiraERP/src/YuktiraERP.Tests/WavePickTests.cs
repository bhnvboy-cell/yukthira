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

public class WavePickTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task WAV01_CreateWave_FromDeliveries()
    {
        var db = CreateDb();
        var service = new WavePickService(db);

        db.Deliveries.Add(new DeliveryEntity
        {
            DeliveryNumber = "DEL-001", CustomerName = "Acme Corp", Status = "Pending"
        });
        db.Deliveries.Add(new DeliveryEntity
        {
            DeliveryNumber = "DEL-002", CustomerName = "Beta Inc", Status = "Pending"
        });
        await db.SaveChangesAsync();

        var result = await service.CreateWaveAsync(new WaveCreateRequest
        {
            WarehouseId = "WH-01",
            WaveName = "Morning Wave",
            WaveType = "Standard",
            Priority = "1",
            DeliveryNumbers = new System.Collections.Generic.List<string> { "DEL-001", "DEL-002" }
        });

        Assert.True(result.Success);
        Assert.NotEmpty(result.WaveNumber);
        Assert.Equal(2, result.TotalLines);
        Assert.True(result.TotalQuantity > 0);

        var wave = await db.WavePicks.FirstOrDefaultAsync(w => w.WaveNumber == result.WaveNumber);
        Assert.NotNull(wave);
        Assert.Equal("Planned", wave.Status);
        Assert.Equal("WH-01", wave.Warehouse);

        var lines = await db.WavePickLines.Where(l => l.WaveId == wave.Id).ToListAsync();
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public async Task WAV02_ReleaseWave_SetsStatus()
    {
        var db = CreateDb();
        var service = new WavePickService(db);

        var createResult = await service.CreateWaveAsync(new WaveCreateRequest
        {
            WarehouseId = "WH-01",
            WaveName = "Test Wave",
            WaveType = "Standard",
            Priority = "3"
        });

        var releaseResult = await service.ReleaseWaveAsync(new WaveReleaseRequest
        {
            WaveId = createResult.WaveId,
            AssignPickersAutomatically = true
        });

        Assert.True(releaseResult.Success);
        Assert.Equal(5, releaseResult.TasksCreated);
        Assert.True(releaseResult.ReleasedAt <= DateTime.UtcNow);

        var wave = await db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == createResult.WaveId);
        Assert.NotNull(wave);
        Assert.Equal("Released", wave.Status);
        Assert.NotNull(wave.ReleaseTime);

        var lines = await db.WavePickLines.Where(l => l.WaveId == wave.Id).ToListAsync();
        Assert.True(lines.All(l => l.Status == "Assigned"));
    }

    [Fact]
    public async Task WAV03_CompletePickLine_UpdatesQty()
    {
        var db = CreateDb();
        var service = new WavePickService(db);

        var createResult = await service.CreateWaveAsync(new WaveCreateRequest
        {
            WarehouseId = "WH-01",
            WaveName = "Complete Test",
            WaveType = "Standard"
        });

        var wave = await db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == createResult.WaveId);
        var line = await db.WavePickLines.FirstOrDefaultAsync(l => l.WaveId == wave.Id);

        var completeResult = await service.CompletePickLineAsync(new WaveCompletePickLineRequest
        {
            WaveId = createResult.WaveId,
            PickLineId = line.Id.ToString(),
            PickedQuantity = line.RequiredQty,
            UserId = "USER-001",
            PickedAt = DateTime.UtcNow
        });

        Assert.True(completeResult.Success);

        var updatedLine = await db.WavePickLines.FindAsync(line.Id);
        Assert.Equal(line.RequiredQty, updatedLine.PickedQty);
        Assert.Equal("Completed", updatedLine.Status);
    }

    [Fact]
    public async Task WAV04_ShortPick_HandlesShortage()
    {
        var db = CreateDb();
        var service = new WavePickService(db);

        var createResult = await service.CreateWaveAsync(new WaveCreateRequest
        {
            WarehouseId = "WH-01",
            WaveName = "Short Pick Test",
            WaveType = "Standard"
        });

        var wave = await db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == createResult.WaveId);
        var line = await db.WavePickLines.FirstOrDefaultAsync(l => l.WaveId == wave.Id);

        var shortPickResult = await service.ShortPickAsync(new WaveShortPickRequest
        {
            WaveId = createResult.WaveId,
            PickLineId = line.Id.ToString(),
            PickedQuantity = line.RequiredQty - 5,
            ShortReason = "Stock not available in bin",
            UserId = "USER-001",
            CreateBackorder = true
        });

        Assert.True(shortPickResult.Success);
        Assert.NotEmpty(shortPickResult.BackorderWaveId);

        var updatedLine = await db.WavePickLines.FindAsync(line.Id);
        Assert.Equal("ShortPicked", updatedLine.Status);
        Assert.Equal(line.RequiredQty - 5, updatedLine.PickedQty);
        Assert.Equal(5, updatedLine.ShortQty);
        Assert.Equal("Stock not available in bin", updatedLine.Notes);

        var backorderWave = await db.WavePicks.FirstOrDefaultAsync(w => w.Id.ToString() == shortPickResult.BackorderWaveId);
        Assert.NotNull(backorderWave);
        Assert.Equal("Backorder", backorderWave.WaveType);
    }
}
