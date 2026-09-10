using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class ZqmQaWorklistService : IZqmQaWorklistService
{
    private readonly YuktiraDbContext _db;

    public ZqmQaWorklistService(YuktiraDbContext db) => _db = db;

    public async Task<List<InspectionLotSelectionResultDto>> GetWorklistAsync(QaWorklistFilter filter, Guid tenantId)
    {
        var query = _db.InspectionLots.AsQueryable();

        if (!string.IsNullOrEmpty(filter.PlantFrom))
            query = query.Where(l => l.Plant.CompareTo(filter.PlantFrom) >= 0);
        if (!string.IsNullOrEmpty(filter.PlantTo))
            query = query.Where(l => l.Plant.CompareTo(filter.PlantTo) <= 0);
        if (!string.IsNullOrEmpty(filter.MaterialFrom))
            query = query.Where(l => l.MaterialCode.CompareTo(filter.MaterialFrom) >= 0);
        if (!string.IsNullOrEmpty(filter.MaterialTo))
            query = query.Where(l => l.MaterialCode.CompareTo(filter.MaterialTo) <= 0);
        if (!string.IsNullOrEmpty(filter.BatchNumber))
            query = query.Where(l => l.BatchNumber == filter.BatchNumber);
        if (!string.IsNullOrEmpty(filter.InspectionType))
            query = query.Where(l => l.InspectionType == filter.InspectionType);
        if (!string.IsNullOrEmpty(filter.AssignedInspector))
            query = query.Where(l => l.AssignedInspector == filter.AssignedInspector);
        if (filter.CreatedFrom.HasValue)
            query = query.Where(l => l.CreatedAt >= filter.CreatedFrom.Value);
        if (filter.CreatedTo.HasValue)
            query = query.Where(l => l.CreatedAt <= filter.CreatedTo.Value);

        if (filter.PendingUDOnly)
        {
            query = query.Where(l => l.Status != "UsageDecisionCompleted" && l.Status != "USAGE_DECIDED");
        }

        if (filter.ViewMode == "QA32")
        {
            query = query.Where(l => l.Status == "Created" || l.Status == "InInspection" || l.Status == "ResultsRecorded");
        }

        var lots = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(filter.MaxHits)
            .ToListAsync();

        var results = new List<InspectionLotSelectionResultDto>();
        foreach (var lot in lots)
        {
            var ud = await _db.UsageDecisions.FirstOrDefaultAsync(u => u.LotNumber == lot.LotNumber);
            results.Add(new InspectionLotSelectionResultDto
            {
                Id = lot.Id,
                LotNumber = lot.LotNumber,
                MaterialCode = lot.MaterialCode,
                MaterialName = lot.MaterialName,
                Plant = lot.Plant,
                BatchNumber = lot.BatchNumber,
                InspectionType = lot.InspectionType,
                Quantity = lot.Quantity,
                BaseUOM = lot.BaseUOM,
                Status = lot.Status,
                AssignedInspector = lot.AssignedInspector,
                SampleSize = lot.SampleSize,
                Inspected = lot.Inspected,
                Passed = lot.Passed,
                Failed = lot.Failed,
                CreatedAt = lot.CreatedAt,
                HasUsageDecision = ud != null,
                UDCode = ud?.UDCode,
                UDDecision = ud?.Decision,
                DecisionDate = ud?.DecisionDate
            });
        }

        return results;
    }

    public async Task<int> GetWorklistCountAsync(QaWorklistFilter filter, Guid tenantId)
    {
        var query = _db.InspectionLots.AsQueryable();

        if (!string.IsNullOrEmpty(filter.PlantFrom))
            query = query.Where(l => l.Plant.CompareTo(filter.PlantFrom) >= 0);
        if (!string.IsNullOrEmpty(filter.MaterialFrom))
            query = query.Where(l => l.MaterialCode.CompareTo(filter.MaterialFrom) >= 0);
        if (filter.PendingUDOnly)
            query = query.Where(l => l.Status != "UsageDecisionCompleted" && l.Status != "USAGE_DECIDED");

        return await query.CountAsync();
    }
}
