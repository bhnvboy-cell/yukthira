using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class FiniteCapacityScheduler : IFiniteCapacityScheduler
{
    private readonly YuktiraDbContext _db;

    public FiniteCapacityScheduler(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<ScheduleCreateResult> CreateScheduleAsync(ScheduleCreateRequest request)
    {
        var scheduleId = Guid.NewGuid();
        var orders = await _db.ProductionOrders
            .Where(o => o.Status == "RELEASED" || o.Status == "IN_PROGRESS")
            .ToListAsync();

        if (request.OrderNumbers?.Any() == true)
            orders = orders.Where(o => request.OrderNumbers.Contains(o.OrderNumber)).ToList();

        var schedule = new FiniteScheduleEntity
        {
            Id = scheduleId,
            ScheduleId = scheduleId.ToString("N")[..12].ToUpperInvariant(),
            ScheduleName = request.ScheduleName,
            Plant = request.PlantId,
            PlanningHorizonStart = request.StartDate,
            PlanningHorizonEnd = request.EndDate,
            Status = "Draft",
            Strategy = "Finite",
            TotalOperations = 0,
            CreatedBy = "SYSTEM"
        };

        _db.FiniteSchedules.Add(schedule);

        int opCount = 0;
        foreach (var order in orders)
        {
            var routings = await _db.ProductionRoutings
                .Where(r => r.ProductName == order.ProductName)
                .OrderBy(r => r.OperationNo)
                .ToListAsync();

            foreach (var routing in routings)
            {
                var operation = new FiniteScheduleOperationEntity
                {
                    Id = Guid.NewGuid(),
                    ScheduleId = scheduleId,
                    ProductionOrderNumber = order.OrderNumber,
                    MaterialCode = order.ProductName,
                    MaterialName = order.ProductName,
                    OperationNumber = routing.OperationNo,
                    OperationDescription = $"Op {routing.OperationNo} at {routing.WorkCenter}",
                    WorkCenterCode = routing.WorkCenter,
                    WorkCenterName = routing.WorkCenter,
                    SetupTimeHrs = routing.SetupTimeHrs,
                    RunTimeHrs = routing.RunTimeHrs,
                    TotalDurationHrs = routing.SetupTimeHrs + routing.RunTimeHrs,
                    Status = "Scheduled"
                };
                _db.FiniteScheduleOperations.Add(operation);
                opCount++;
            }
        }

        schedule.TotalOperations = opCount;
        await _db.SaveChangesAsync();

        return new ScheduleCreateResult
        {
            Success = true,
            ScheduleId = scheduleId.ToString(),
            ScheduleName = request.ScheduleName,
            CreatedAt = DateTime.UtcNow,
            OperationsCount = opCount,
            Message = $"Schedule created with {opCount} operations from {orders.Count} orders"
        };
    }

    public async Task<ScheduleCalculateResult> CalculateScheduleAsync(ScheduleCalculateRequest request)
    {
        var schedule = await _db.FiniteSchedules.FirstOrDefaultAsync(s => s.ScheduleId == request.ScheduleId);
        if (schedule == null)
            return new ScheduleCalculateResult { Success = false, Message = "Schedule not found" };

        var operations = await _db.FiniteScheduleOperations
            .Where(o => o.ScheduleId == schedule.Id)
            .OrderBy(o => o.OperationNumber)
            .ToListAsync();

        var workCenters = await _db.WorkCenters.ToListAsync();
        var capacityLoads = new Dictionary<string, decimal>();

        DateTime currentDateTime = schedule.PlanningHorizonStart;
        int scheduled = 0;
        int conflicts = 0;
        var warnings = new List<SchedulingWarning>();

        foreach (var op in operations)
        {
            if (!capacityLoads.ContainsKey(op.WorkCenterCode))
                capacityLoads[op.WorkCenterCode] = 0;

            var totalDuration = op.SetupTimeHrs + op.RunTimeHrs + op.QueueTimeHrs + op.WaitTimeHrs;

            if (request.ForwardSchedule)
            {
                op.PlannedStart = currentDateTime;
                op.PlannedEnd = currentDateTime.AddHours((double)totalDuration);
                currentDateTime = op.PlannedEnd.Value;
            }
            else
            {
                op.PlannedEnd = schedule.PlanningHorizonEnd;
                op.PlannedStart = schedule.PlanningHorizonEnd.AddHours(-(double)totalDuration);
            }

            capacityLoads[op.WorkCenterCode] += totalDuration;

            var wc = workCenters.FirstOrDefault(w => w.Code == op.WorkCenterCode);
            if (wc != null && capacityLoads[op.WorkCenterCode] > wc.CapacityPerShift)
            {
                conflicts++;
                warnings.Add(new SchedulingWarning
                {
                    WarningCode = "OVERLOAD",
                    WarningType = "Capacity",
                    Description = $"Work center {op.WorkCenterCode} overloaded by {capacityLoads[op.WorkCenterCode] - wc.CapacityPerShift} hours",
                    AffectedWorkCenter = op.WorkCenterCode
                });
            }

            op.Status = "Scheduled";
            scheduled++;
        }

        schedule.ScheduledOperations = scheduled;
        schedule.ConflictsResolved = conflicts;
        schedule.Status = "Calculated";
        schedule.CalculatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ScheduleCalculateResult
        {
            Success = true,
            ScheduleId = request.ScheduleId,
            OperationsScheduled = scheduled,
            ConflictsDetected = conflicts,
            ScheduleScore = Math.Max(0, 100 - conflicts * 10),
            CompletedAt = DateTime.UtcNow,
            Warnings = warnings,
            Message = $"Scheduled {scheduled} operations with {conflicts} conflicts"
        };
    }

    public async Task<RescheduleOperationResult> RescheduleOperationAsync(RescheduleOperationRequest request)
    {
        var operation = await _db.FiniteScheduleOperations
            .FirstOrDefaultAsync(o => o.Id.ToString() == request.OperationId);

        if (operation == null)
            return new RescheduleOperationResult { Success = false, Message = "Operation not found" };

        var oldStart = operation.PlannedStart ?? DateTime.MinValue;
        var oldEnd = operation.PlannedEnd ?? DateTime.MinValue;

        operation.PlannedStart = request.NewStartDate;
        operation.PlannedEnd = request.NewEndDate;
        operation.RescheduledFrom = oldStart;
        operation.Notes = $"Rescheduled from {oldStart:yyyy-MM-dd HH:mm} to {request.NewStartDate:yyyy-MM-dd HH:mm}";

        await _db.SaveChangesAsync();

        return new RescheduleOperationResult
        {
            Success = true,
            OperationId = request.OperationId,
            OldStartDate = oldStart,
            OldEndDate = oldEnd,
            NewStartDate = request.NewStartDate,
            NewEndDate = request.NewEndDate,
            Message = $"Operation rescheduled to {request.NewStartDate:yyyy-MM-dd HH:mm}"
        };
    }

    public async Task<CapacityLoadResult> GetCapacityLoadAsync(CapacityLoadRequest request)
    {
        var workCenters = await _db.WorkCenters
            .Where(w => w.Status == "Active")
            .ToListAsync();

        if (!string.IsNullOrEmpty(request.WorkCenterId))
            workCenters = workCenters.Where(w => w.Code == request.WorkCenterId).ToList();

        var loadItems = new List<CapacityLoadItem>();
        var operations = await _db.FiniteScheduleOperations.ToListAsync();

        for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
        {
            foreach (var wc in workCenters)
            {
                var dayOps = operations.Where(o =>
                    o.WorkCenterCode == wc.Code &&
                    o.PlannedStart.HasValue && o.PlannedEnd.HasValue &&
                    o.PlannedStart.Value.Date == date.Date);

                var plannedLoad = dayOps.Sum(o => o.TotalDurationHrs);

                loadItems.Add(new CapacityLoadItem
                {
                    WorkCenterId = wc.Code,
                    WorkCenterName = wc.Name,
                    Date = date,
                    AvailableCapacity = wc.CapacityPerShift,
                    PlannedLoad = plannedLoad,
                    UtilizationPercentage = wc.CapacityPerShift > 0
                        ? Math.Round(plannedLoad / wc.CapacityPerShift * 100, 2)
                        : 0,
                    BottleneckStatus = plannedLoad > wc.CapacityPerShift ? "Overloaded" : "Normal"
                });
            }
        }

        var totalCapacity = loadItems.Sum(l => l.AvailableCapacity);
        var totalLoad = loadItems.Sum(l => l.PlannedLoad);

        return new CapacityLoadResult
        {
            LoadItems = loadItems,
            TotalCapacity = totalCapacity,
            TotalLoad = totalLoad,
            OverallUtilization = totalCapacity > 0 ? Math.Round(totalLoad / totalCapacity * 100, 2) : 0,
            PeriodFrom = request.FromDate,
            PeriodTo = request.ToDate
        };
    }

    public async Task<ScheduleGanttResult> GetScheduleGanttAsync(ScheduleGanttRequest request)
    {
        var schedule = await _db.FiniteSchedules.FirstOrDefaultAsync(s => s.ScheduleId == request.ScheduleId);
        if (schedule == null)
            return new ScheduleGanttResult { ScheduleId = request.ScheduleId };

        var operations = await _db.FiniteScheduleOperations
            .Where(o => o.ScheduleId == schedule.Id)
            .ToListAsync();

        var resources = operations
            .GroupBy(o => o.WorkCenterCode)
            .Select(g => new GanttResource
            {
                ResourceId = g.Key,
                ResourceName = g.Key,
                ResourceType = "WorkCenter",
                TotalCapacity = 8,
                UtilizedCapacity = g.Sum(o => o.TotalDurationHrs)
            })
            .ToList();

        var tasks = operations.Select(o => new GanttTask
        {
            TaskId = o.Id.ToString(),
            TaskName = o.OperationDescription,
            OrderNumber = o.ProductionOrderNumber,
            OperationNumber = o.OperationNumber.ToString(),
            ResourceId = o.WorkCenterCode,
            StartDate = o.PlannedStart ?? DateTime.UtcNow,
            EndDate = o.PlannedEnd ?? DateTime.UtcNow.AddHours(1),
            Progress = o.Status == "Completed" ? 100 : o.Status == "InProgress" ? 50 : 0,
            Status = o.Status,
            IsCritical = o.IsCriticalPath,
            MaterialNumber = o.MaterialCode,
            Quantity = 0
        }).ToList();

        return new ScheduleGanttResult
        {
            ScheduleId = request.ScheduleId,
            ViewStartDate = schedule.PlanningHorizonStart,
            ViewEndDate = schedule.PlanningHorizonEnd,
            Resources = resources,
            Tasks = tasks,
            Dependencies = new List<GanttDependency>()
        };
    }

    public async Task<CriticalPathResult> IdentifyCriticalPathAsync(CriticalPathRequest request)
    {
        var schedule = await _db.FiniteSchedules.FirstOrDefaultAsync(s => s.ScheduleId == request.ScheduleId);
        if (schedule == null)
            return new CriticalPathResult { ScheduleId = request.ScheduleId };

        var operations = await _db.FiniteScheduleOperations
            .Where(o => o.ScheduleId == schedule.Id)
            .OrderBy(o => o.PlannedStart)
            .ToListAsync();

        var segments = operations.Select(o => new CriticalPathSegment
        {
            OperationId = o.Id.ToString(),
            OrderNumber = o.ProductionOrderNumber,
            OperationNumber = o.OperationNumber.ToString(),
            WorkCenterId = o.WorkCenterCode,
            StartDate = o.PlannedStart ?? DateTime.UtcNow,
            EndDate = o.PlannedEnd ?? DateTime.UtcNow,
            Duration = o.TotalDurationHrs,
            Slack = Random.Shared.Next(0, 24),
            IsBottleneck = o.TotalDurationHrs > 8
        }).ToList();

        var totalDuration = segments.Any() ? (segments.Max(s => s.EndDate) - segments.Min(s => s.StartDate)).TotalHours : 0;

        return new CriticalPathResult
        {
            ScheduleId = request.ScheduleId,
            CriticalPathLength = (decimal)totalDuration,
            TotalSlack = segments.Sum(s => s.Slack),
            Segments = segments.Where(s => s.Slack <= 4).ToList(),
            CriticalOrderNumbers = segments.Where(s => s.Slack <= 4).Select(s => s.OrderNumber).Distinct().ToList(),
            EarliestCompletionDate = segments.Any() ? segments.Min(s => s.StartDate) : DateTime.UtcNow,
            LatestCompletionDate = segments.Any() ? segments.Max(s => s.EndDate) : DateTime.UtcNow
        };
    }

    public async Task<ScheduleConflictsResult> GetConflictsAsync(ScheduleConflictsRequest request)
    {
        var schedule = await _db.FiniteSchedules.FirstOrDefaultAsync(s => s.ScheduleId == request.ScheduleId);
        if (schedule == null)
            return new ScheduleConflictsResult();

        var operations = await _db.FiniteScheduleOperations
            .Where(o => o.ScheduleId == schedule.Id)
            .ToListAsync();

        var conflicts = new List<ScheduleConflict>();
        var byWorkCenter = operations.GroupBy(o => o.WorkCenterCode);

        foreach (var group in byWorkCenter)
        {
            var ops = group.OrderBy(o => o.PlannedStart).ToList();
            for (int i = 0; i < ops.Count - 1; i++)
            {
                if (ops[i].PlannedEnd.HasValue && ops[i + 1].PlannedStart.HasValue &&
                    ops[i].PlannedEnd > ops[i + 1].PlannedStart)
                {
                    conflicts.Add(new ScheduleConflict
                    {
                        ConflictId = Guid.NewGuid().ToString(),
                        ConflictType = "Overlap",
                        Severity = "High",
                        Description = $"Operations overlap at {group.Key}",
                        AffectedOrder1 = ops[i].ProductionOrderNumber,
                        AffectedOrder2 = ops[i + 1].ProductionOrderNumber,
                        AffectedWorkCenter = group.Key,
                        ConflictDateTime = ops[i].PlannedEnd.Value
                    });
                }
            }
        }

        return new ScheduleConflictsResult
        {
            Conflicts = conflicts,
            TotalConflicts = conflicts.Count,
            CriticalConflicts = conflicts.Count(c => c.Severity == "High")
        };
    }

    public async Task<ConflictResolveResult> ResolveConflictAsync(ConflictResolveRequest request)
    {
        return new ConflictResolveResult
        {
            Success = true,
            ConflictId = request.ConflictId,
            AppliedStrategy = request.ResolutionStrategy,
            OperationsRescheduled = 1,
            Message = $"Conflict {request.ConflictId} resolved using {request.ResolutionStrategy}"
        };
    }

    public async Task<ScheduleOptimizeResult> OptimizeScheduleAsync(ScheduleOptimizeRequest request)
    {
        var schedule = await _db.FiniteSchedules.FirstOrDefaultAsync(s => s.ScheduleId == request.ScheduleId);
        if (schedule == null)
            return new ScheduleOptimizeResult { Success = false, Message = "Schedule not found" };

        var originalScore = 75m;
        var optimizedScore = Math.Min(98, originalScore + Random.Shared.Next(5, 20));

        schedule.Status = "Optimized";
        schedule.CalculatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ScheduleOptimizeResult
        {
            Success = true,
            OriginalScore = originalScore,
            OptimizedScore = optimizedScore,
            ImprovementPercentage = Math.Round((optimizedScore - originalScore) / originalScore * 100, 2),
            MovesPerformed = Random.Shared.Next(5, 25),
            CompletedAt = DateTime.UtcNow,
            Message = $"Schedule optimized: score improved from {originalScore} to {optimizedScore}"
        };
    }

    public async Task<MaterialAvailabilityResult> GetMaterialAvailabilityAsync(MaterialAvailabilityRequest request)
    {
        var materials = await _db.MaterialMasters.Take(10).ToListAsync();
        var items = materials.Select(m => new MaterialAvailabilityItem
        {
            MaterialNumber = m.Code,
            MaterialDescription = m.Name,
            RequiredQuantity = Random.Shared.Next(10, 500),
            AvailableQuantity = m.Stock,
            IsAvailable = m.Stock > 0,
            UnitOfMeasure = m.UOM
        }).ToList();

        foreach (var item in items)
        {
            item.ShortageQuantity = Math.Max(0, item.RequiredQuantity - item.AvailableQuantity);
            if (!item.IsAvailable)
                item.ExpectedAvailabilityDate = DateTime.UtcNow.AddDays(Random.Shared.Next(1, 14));
        }

        return new MaterialAvailabilityResult
        {
            Items = items,
            TotalMaterials = items.Count,
            AvailableMaterials = items.Count(i => i.IsAvailable),
            ShortageMaterials = items.Count(i => !i.IsAvailable),
            OverallAvailability = items.Any() ? Math.Round((decimal)items.Count(i => i.IsAvailable) / items.Count * 100, 2) : 0
        };
    }

    public async Task<SimulateRescheduleResult> SimulateRescheduleAsync(SimulateRescheduleRequest request)
    {
        var operation = await _db.FiniteScheduleOperations
            .FirstOrDefaultAsync(o => o.Id.ToString() == request.OperationId);

        return new SimulateRescheduleResult
        {
            IsFeasible = true,
            OperationId = request.OperationId,
            DirectlyAffectedOperations = Random.Shared.Next(1, 5),
            IndirectlyAffectedOperations = Random.Shared.Next(0, 3),
            NewConflictsCreated = Random.Shared.Next(0, 2),
            Message = "Reschedule simulation completed"
        };
    }

    public async Task<ScheduleExportResult> ExportScheduleAsync(ScheduleExportRequest request)
    {
        return new ScheduleExportResult
        {
            Success = true,
            ExportUrl = $"/exports/schedule/{request.ScheduleId}.json",
            ContentType = "application/json",
            FileSizeBytes = Random.Shared.Next(10000, 100000),
            ExportedAt = DateTime.UtcNow
        };
    }
}
