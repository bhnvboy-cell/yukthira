using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class CapacityPlanningService : ICapacityPlanningService
{
    private readonly YuktiraDbContext _db;
    private const decimal HoursPerShift = 8;
    private const int ShiftsPerDay = 1;

    public CapacityPlanningService(YuktiraDbContext db) { _db = db; }

    public async Task<List<WorkCenterLoadDto>> CalculateLoadAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var workCenters = await _db.Set<WorkCenterEntity>().ToListAsync();
        var routings = await _db.Set<ProductionRoutingEntity>().Where(r => r.Status == "Active").ToListAsync();
        var prodOrders = await _db.Set<ProductionOrderEntity>().Where(o => o.Status == "Planned" || o.Status == "In Progress").ToListAsync();
        var result = new List<WorkCenterLoadDto>();

        var start = startDate ?? DateTime.Today;
        var end = endDate ?? start.AddDays(14);
        var workingDays = Math.Max(1, (int)(end - start).TotalDays + 1);

        foreach (var wc in workCenters)
        {
            var availableHours = workingDays * ShiftsPerDay * HoursPerShift;
            var operations = new List<OperationLoadDetailDto>();
            decimal requiredHours = 0;

            foreach (var order in prodOrders)
            {
                var orderRoutings = routings.Where(r => r.ProductName == order.ProductName && r.WorkCenter == wc.Name).ToList();
                foreach (var rtg in orderRoutings)
                {
                    var setupHrs = rtg.SetupTimeHrs;
                    var runHrs = rtg.RunTimeHrs * order.Quantity;
                    var total = setupHrs + runHrs;
                    requiredHours += total;

                    operations.Add(new OperationLoadDetailDto
                    {
                        ProductName = order.ProductName,
                        ProductionOrderNumber = order.OrderNumber,
                        OperationNo = rtg.OperationNo,
                        WorkCenterCode = wc.Code,
                        SetupTimeHrs = rtg.SetupTimeHrs,
                        RunTimePerUnitHrs = rtg.RunTimeHrs,
                        Quantity = order.Quantity,
                        TotalRequiredHours = total,
                        StartDate = order.StartDate,
                        EndDate = order.EndDate
                    });
                }
            }

            var loadPercent = availableHours > 0 ? Math.Round(requiredHours / availableHours * 100, 1) : 0;
            result.Add(new WorkCenterLoadDto
            {
                WorkCenterId = wc.Id,
                Code = wc.Code,
                Name = wc.Name,
                Department = wc.Department,
                AvailableHours = availableHours,
                RequiredHours = requiredHours,
                LoadPercent = loadPercent,
                OperationCount = operations.Count,
                Status = wc.Status,
                Operations = operations
            });
        }
        return result;
    }

    public async Task<WorkCenterLoadDto?> GetWorkCenterLoadAsync(Guid workCenterId, DateTime startDate, DateTime endDate)
    {
        var loads = await CalculateLoadAsync(Guid.Empty, startDate, endDate);
        return loads.FirstOrDefault(l => l.WorkCenterId == workCenterId);
    }

    public async Task<List<OperationLoadDetailDto>> GetOperationsForWorkCenterAsync(Guid workCenterId, DateTime startDate, DateTime endDate)
    {
        var loads = await CalculateLoadAsync(Guid.Empty, startDate, endDate);
        return loads.Where(l => l.WorkCenterId == workCenterId).SelectMany(l => l.Operations).ToList();
    }
}
