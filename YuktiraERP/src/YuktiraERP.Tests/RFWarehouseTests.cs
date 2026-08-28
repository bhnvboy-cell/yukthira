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

public class RFWarehouseTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task RF01_StartSession_CreatesSession()
    {
        var db = CreateDb();
        var service = new RFWarehouseService(db);

        var result = await service.StartSessionAsync(new RFSessionStartRequest
        {
            WarehouseId = "WH-01",
            UserId = "OPERATOR-001",
            DeviceId = "SCAN-DEVICE-01",
            StationId = "STATION-A"
        });

        Assert.True(result.Success);
        Assert.NotEmpty(result.SessionId);
        Assert.Equal("WH-01", result.WarehouseName);
        Assert.True(result.StartedAt <= DateTime.UtcNow);

        var session = await db.RFSessions.FirstOrDefaultAsync(s => s.SessionId == result.SessionId);
        Assert.NotNull(session);
        Assert.Equal("Active", session.Status);
        Assert.Equal("OPERATOR-001", session.UserId);
        Assert.Equal("WH-01", session.Warehouse);
    }

    [Fact]
    public async Task RF02_PostPick_UpdatesTask()
    {
        var db = CreateDb();
        var service = new RFWarehouseService(db);

        var sessionResult = await service.StartSessionAsync(new RFSessionStartRequest
        {
            WarehouseId = "WH-01",
            UserId = "OPERATOR-001",
            DeviceId = "SCAN-01",
            StationId = "S1"
        });

        var pickTask = new RFPickTaskEntity
        {
            TaskId = "PICK-001",
            MaterialCode = "MAT-001",
            MaterialName = "Steel Rod",
            SourceBin = "A-01-01",
            DestinationBin = "B-01-01",
            RequiredQty = 100,
            PickedQty = 0,
            UnitOfMeasure = "EA",
            Status = "Open",
            AssignedTo = "OPERATOR-001"
        };
        db.RFPickTasks.Add(pickTask);
        await db.SaveChangesAsync();

        var result = await service.PostPickAsync(new RFPickPostRequest
        {
            SessionId = sessionResult.SessionId,
            TaskId = "PICK-001",
            Material = "MAT-001",
            SourceBin = "A-01-01",
            Quantity = 30,
            UnitOfMeasure = "EA",
            DestinationBin = "B-01-01"
        });

        Assert.True(result.Success);
        Assert.Equal(30, result.PickedQuantity);

        var updatedTask = await db.RFPickTasks.FirstOrDefaultAsync(t => t.TaskId == "PICK-001");
        Assert.NotNull(updatedTask);
        Assert.Equal(30, updatedTask.PickedQty);
        Assert.Equal("InProgress", updatedTask.Status);
        Assert.Equal(1, updatedTask.ScanCount);
    }

    [Fact]
    public async Task RF03_SessionSummary_ReturnsCounts()
    {
        var db = CreateDb();
        var service = new RFWarehouseService(db);

        var sessionResult = await service.StartSessionAsync(new RFSessionStartRequest
        {
            WarehouseId = "WH-01",
            UserId = "OPERATOR-001",
            DeviceId = "SCAN-01",
            StationId = "S1"
        });

        var session = await db.RFSessions.FirstOrDefaultAsync(s => s.SessionId == sessionResult.SessionId);
        var pickTask = new RFPickTaskEntity
        {
            TaskId = "PICK-001", MaterialCode = "M1", MaterialName = "Material1",
            SourceBin = "A1", DestinationBin = "B1", RequiredQty = 50,
            UnitOfMeasure = "EA", Status = "Open", AssignedTo = "OPERATOR-001"
        };
        db.RFPickTasks.Add(pickTask);
        await db.SaveChangesAsync();

        await service.PostPickAsync(new RFPickPostRequest
        {
            SessionId = sessionResult.SessionId, TaskId = "PICK-001",
            Material = "M1", SourceBin = "A1", Quantity = 25, UnitOfMeasure = "EA", DestinationBin = "B1"
        });

        await service.PostPickAsync(new RFPickPostRequest
        {
            SessionId = sessionResult.SessionId, TaskId = "PICK-001",
            Material = "M1", SourceBin = "A1", Quantity = 25, UnitOfMeasure = "EA", DestinationBin = "B1"
        });

        var summary = await service.GetSessionSummaryAsync(new RFGetSessionSummaryRequest
        {
            SessionId = sessionResult.SessionId
        });

        Assert.Equal(sessionResult.SessionId, summary.SessionId);
        Assert.Equal("OPERATOR-001", summary.UserId);
        Assert.Equal(2, summary.TasksStarted);
        Assert.Equal(50, summary.UnitsPicked);
        Assert.Equal(2, summary.ScanCount);
        Assert.Equal(0, summary.ErrorCount);
    }
}
