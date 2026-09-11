using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class CapacityGapService : ICapacityGapService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<CapacityGapService> _logger;

    public CapacityGapService(YuktiraDbContext db, ILogger<CapacityGapService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CapacityEvaluationResult> EvaluateCapacityAsync(CapacityEvaluationRequest request)
    {
        try
        {
            var workCenter = await _db.WorkCenters.FirstOrDefaultAsync(w => w.Code == request.WorkCenterCode);
            if (workCenter == null)
                return new CapacityEvaluationResult { Success = false, Errors = { $"Work center not found: {request.WorkCenterCode}" } };

            var dailyCapacity = workCenter.CapacityPerDay;
            var days = (request.ToDate - request.FromDate).Days + 1;
            var totalCapacity = dailyCapacity * days;

            var orders = await _db.ProductionOrders
                .Where(o => o.Plant == request.WorkCenterCode && o.Status != "TECO" && o.Status != "CANCELLED")
                .ToListAsync();

            var segments = new List<CapacitySegmentDto>();
            var assignedHours = 0m;

            for (int i = 0; i < days; i++)
            {
                var date = request.FromDate.AddDays(i);
                var dayOrders = orders.Where(o => o.StartDate <= date && o.EndDate >= date).ToList();
                var dayAssigned = dayOrders.Sum(o => o.Quantity);
                var utilization = totalCapacity > 0 ? (dayAssigned / dailyCapacity) * 100 : 0;

                segments.Add(new CapacitySegmentDto
                {
                    Date = date,
                    AvailableHours = dailyCapacity,
                    AssignedHours = dayAssigned,
                    UtilizationPercent = Math.Round(utilization, 1),
                    OverloadedOrder = utilization > 100 ? dayOrders.FirstOrDefault()?.OrderNumber : null
                });

                assignedHours += dayAssigned;
            }

            var avgUtilization = totalCapacity > 0 ? (assignedHours / totalCapacity) * 100 : 0;
            var loadStatus = avgUtilization > 100 ? CapacityLoadStatus.Critical
                           : avgUtilization > 85 ? CapacityLoadStatus.Overloaded
                           : avgUtilization > 60 ? CapacityLoadStatus.Optimal
                           : CapacityLoadStatus.Underloaded;

            var overloadWarnings = segments.Where(s => s.UtilizationPercent > 100)
                .Select(s => $"Overload on {s.Date:yyyy-MM-dd}: {s.UtilizationPercent}% utilized").ToList();

            return new CapacityEvaluationResult
            {
                Success = true,
                WorkCenterCode = request.WorkCenterCode,
                AvailableCapacity = totalCapacity,
                AssignedCapacity = assignedHours,
                UtilizationPercent = Math.Round(avgUtilization, 1),
                LoadStatus = loadStatus,
                Segments = segments,
                OverloadWarnings = overloadWarnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capacity evaluation failed for {WorkCenter}", request.WorkCenterCode);
            return new CapacityEvaluationResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<List<CapacitySegmentDto>> GetCapacityOverviewAsync(string workCenterCode, DateTime from, DateTime to, Guid tenantId)
    {
        return Task.FromResult(new List<CapacitySegmentDto>());
    }
}

public class KanbanService : IKanbanService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<KanbanService> _logger;

    public KanbanService(YuktiraDbContext db, ILogger<KanbanService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<KanbanBoardResult> GetKanbanBoardAsync(KanbanBoardRequest request)
    {
        try
        {
            var bins = await _db.Bins
                .Where(b => b.WarehouseNumber == request.Plant)
                .ToListAsync();

            var items = bins.Select(b => new KanbanBoardItemDto
            {
                BinCode = b.BinCode,
                Status = b.CurrentWeight >= b.MaxWeight * 0.9m ? KanbanStatus.Full
                       : b.CurrentWeight <= b.MaxWeight * 0.1m ? KanbanStatus.Empty
                       : KanbanStatus.InProcess,
                CurrentQuantity = b.CurrentWeight,
                TargetQuantity = b.MaxWeight
            }).ToList();

            return new KanbanBoardResult
            {
                Success = true,
                Items = items,
                TotalInventory = items.Sum(i => i.CurrentQuantity),
                ReplenishmentQuantity = items.Where(i => i.Status == KanbanStatus.Empty).Sum(i => i.TargetQuantity - i.CurrentQuantity)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kanban board retrieval failed");
            return new KanbanBoardResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<bool> TriggerReplenishmentAsync(string binCode, string materialCode, decimal quantity, Guid tenantId)
    {
        _logger.LogInformation("Replenishment triggered: Bin={Bin}, Material={Material}, Qty={Qty}", binCode, materialCode, quantity);
        return Task.FromResult(true);
    }
}
