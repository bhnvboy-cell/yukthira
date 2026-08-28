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

public class FiniteCapacityTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task FCS01_CreateSchedule_FromOrders()
    {
        var db = CreateDb();
        var service = new FiniteCapacityScheduler(db);

        db.WorkCenters.Add(new WorkCenterEntity { Code = "WC-01", Name = "CNC Machine", CapacityPerShift = 8, Status = "Active" });
        db.ProductionOrders.Add(new ProductionOrderEntity
        {
            OrderNumber = "PO-001", ProductName = "Widget-A", Quantity = 100,
            StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "RELEASED"
        });
        db.ProductionRoutings.Add(new ProductionRoutingEntity
        {
            ProductName = "Widget-A", OperationNo = 10, WorkCenter = "WC-01",
            SetupTimeHrs = 1, RunTimeHrs = 4, Status = "Active"
        });
        await db.SaveChangesAsync();

        var result = await service.CreateScheduleAsync(new ScheduleCreateRequest
        {
            PlantId = "P1",
            ScheduleName = "Production Schedule Jan 2026",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            OrderNumbers = new System.Collections.Generic.List<string> { "PO-001" }
        });

        Assert.True(result.Success);
        Assert.NotEmpty(result.ScheduleId);
        Assert.Equal(1, result.OperationsCount);

        var schedule = await db.FiniteSchedules.FirstOrDefaultAsync(s => s.Id.ToString() == result.ScheduleId);
        Assert.NotNull(schedule);
        Assert.Equal("Draft", schedule.Status);
        Assert.Equal("P1", schedule.Plant);
    }

    [Fact]
    public async Task FCS02_CalculateSchedule_SetsOperations()
    {
        var db = CreateDb();
        var service = new FiniteCapacityScheduler(db);

        db.WorkCenters.Add(new WorkCenterEntity { Code = "WC-01", Name = "CNC", CapacityPerShift = 8, Status = "Active" });

        var scheduleId = Guid.NewGuid();
        db.FiniteSchedules.Add(new FiniteScheduleEntity
        {
            Id = scheduleId, ScheduleId = "SCHED-001", ScheduleName = "Test",
            Plant = "P1", PlanningHorizonStart = DateTime.UtcNow, PlanningHorizonEnd = DateTime.UtcNow.AddDays(14),
            Status = "Draft"
        });
        db.FiniteScheduleOperations.Add(new FiniteScheduleOperationEntity
        {
            ScheduleId = scheduleId, ProductionOrderNumber = "PO-001",
            OperationNumber = 10, WorkCenterCode = "WC-01",
            SetupTimeHrs = 1, RunTimeHrs = 4, Status = "Scheduled"
        });
        await db.SaveChangesAsync();

        var result = await service.CalculateScheduleAsync(new ScheduleCalculateRequest
        {
            ScheduleId = "SCHED-001",
            ForwardSchedule = true
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.OperationsScheduled);
        Assert.True(result.ScheduleScore > 0);

        var schedule = await db.FiniteSchedules.FirstOrDefaultAsync(s => s.ScheduleId == "SCHED-001");
        Assert.Equal("Calculated", schedule.Status);
        Assert.NotNull(schedule.CalculatedAt);

        var op = await db.FiniteScheduleOperations.FirstOrDefaultAsync(o => o.ScheduleId == scheduleId);
        Assert.NotNull(op.PlannedStart);
        Assert.NotNull(op.PlannedEnd);
        Assert.True(op.PlannedEnd > op.PlannedStart);
    }

    [Fact]
    public async Task FCS03_CapacityLoad_CalculatesCorrectly()
    {
        var db = CreateDb();
        var service = new FiniteCapacityScheduler(db);

        db.WorkCenters.Add(new WorkCenterEntity { Code = "WC-01", Name = "CNC", CapacityPerShift = 8, Status = "Active" });

        var scheduleId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;
        db.FiniteSchedules.Add(new FiniteScheduleEntity
        {
            Id = scheduleId, ScheduleId = "SCHED-002", ScheduleName = "Load Test",
            Plant = "P1", PlanningHorizonStart = today, PlanningHorizonEnd = today.AddDays(3),
            Status = "Draft"
        });
        db.FiniteScheduleOperations.Add(new FiniteScheduleOperationEntity
        {
            ScheduleId = scheduleId, ProductionOrderNumber = "PO-001",
            WorkCenterCode = "WC-01", PlannedStart = today.AddHours(8),
            PlannedEnd = today.AddHours(12), SetupTimeHrs = 1, RunTimeHrs = 3,
            TotalDurationHrs = 4, Status = "Scheduled"
        });
        await db.SaveChangesAsync();

        var result = await service.GetCapacityLoadAsync(new CapacityLoadRequest
        {
            PlantId = "P1",
            FromDate = today,
            ToDate = today.AddDays(2),
            WorkCenterId = "WC-01"
        });

        Assert.True(result.LoadItems.Count > 0);
        Assert.True(result.TotalCapacity > 0);
        Assert.True(result.OverallUtilization > 0);
        Assert.Equal(today, result.PeriodFrom);
    }
}
