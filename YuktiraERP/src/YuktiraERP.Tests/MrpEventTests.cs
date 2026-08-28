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

public class MrpEventTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task EVT01_PublishEvent_CreatesEvent()
    {
        var db = CreateDb();
        var service = new MrpEventScheduler(db);

        var result = await service.PublishEventAsync(new MrpEventPublishRequest
        {
            EventType = "GOODS_RECEIPT",
            EventSource = "MM",
            PlantId = "P1",
            MaterialNumber = "MAT-001",
            Priority = "High"
        });

        Assert.True(result.Success);
        Assert.NotEmpty(result.EventId);
        Assert.StartsWith("EVT", result.EventId);
        Assert.True(result.PublishedAt <= DateTime.UtcNow);

        var evt = await db.MrpEvents.FirstOrDefaultAsync(e => e.EventId == result.EventId);
        Assert.NotNull(evt);
        Assert.Equal("GOODS_RECEIPT", evt.EventType);
        Assert.Equal("MM", evt.EventSource);
        Assert.Equal("MAT-001", evt.MaterialCode);
        Assert.Equal(1, evt.Priority);
    }

    [Fact]
    public async Task EVT02_RunEventDrivenMrp_ProcessesEvents()
    {
        var db = CreateDb();
        var service = new MrpEventScheduler(db);

        db.MaterialMasters.Add(new MaterialMasterEntity { Code = "MAT-LOW", Name = "Low Stock Material", Stock = 5, Status = "Active" });
        db.MaterialMasters.Add(new MaterialMasterEntity { Code = "MAT-MED", Name = "Medium Stock", Stock = 30, Status = "Active" });
        db.MaterialMasters.Add(new MaterialMasterEntity { Code = "MAT-HIGH", Name = "High Stock", Stock = 200, Status = "Active" });
        await db.SaveChangesAsync();

        var result = await service.RunEventDrivenMrpAsync(new EventDrivenMrpRunRequest
        {
            PlantId = "P1",
            RunType = "EventDriven"
        });

        Assert.True(result.Success);
        Assert.NotEmpty(result.RunId);
        Assert.Equal(3, result.MaterialsProcessed);
        Assert.True(result.PlannedOrdersCreated > 0 || result.PurchaseRequisitionsCreated > 0);
        Assert.Equal("Completed", result.Status);

        var planningRun = await db.MrpPlanningRuns.FirstOrDefaultAsync(r => r.RunId == result.RunId);
        Assert.NotNull(planningRun);
        Assert.Equal("Completed", planningRun.Status);
        Assert.Equal(3, planningRun.MaterialsProcessed);
    }

    [Fact]
    public async Task EVT03_GetEventStream_ReturnsHistory()
    {
        var db = CreateDb();
        var service = new MrpEventScheduler(db);

        db.MrpEventStreams.Add(new MrpEventStreamEntity
        {
            StreamId = "STR-001", MaterialCode = "MAT-001", Plant = "P1",
            EventSequence = 1, EventType = "GOODS_RECEIPT", EventId = "EVT-001",
            RunningDemand = 100, RunningSupply = 50, RunningProjectedBalance = -50,
            SnapshotDate = DateTime.UtcNow.AddDays(-2), IsSnapshot = false
        });
        db.MrpEventStreams.Add(new MrpEventStreamEntity
        {
            StreamId = "STR-002", MaterialCode = "MAT-001", Plant = "P1",
            EventSequence = 2, EventType = "PLANNED_ORDER", EventId = "EVT-002",
            RunningDemand = 100, RunningSupply = 80, RunningProjectedBalance = -20,
            SnapshotDate = DateTime.UtcNow.AddDays(-1), IsSnapshot = true
        });
        db.MrpEventStreams.Add(new MrpEventStreamEntity
        {
            StreamId = "STR-003", MaterialCode = "MAT-002", Plant = "P2",
            EventSequence = 1, EventType = "GOODS_ISSUE", EventId = "EVT-003",
            RunningDemand = 50, RunningSupply = 50, RunningProjectedBalance = 0,
            SnapshotDate = DateTime.UtcNow, IsSnapshot = false
        });
        await db.SaveChangesAsync();

        var result = await service.GetEventStreamAsync(new EventStreamRequest
        {
            MaterialNumber = "MAT-001",
            PageSize = 50
        });

        Assert.Equal(2, result.TotalCount);
        Assert.True(result.Events.Count > 0);
        Assert.True(result.Events.All(e => e.MaterialNumber == "MAT-001"));
        Assert.False(result.HasMore);
    }
}
