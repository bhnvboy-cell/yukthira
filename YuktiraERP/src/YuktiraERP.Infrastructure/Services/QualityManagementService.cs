using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class QualityManagementService : IQualityManagementService
{
    private readonly YuktiraDbContext _db;

    public QualityManagementService(YuktiraDbContext db) => _db = db;

    public async Task<InspectionLotSelectionResponseDto> GetSelectedInspectionLotsAsync(
        InspectionLotSelectionFilterDto filter, Guid tenantId)
    {
        var query = _db.InspectionLots.AsQueryable();

        if (!string.IsNullOrEmpty(filter.PlantFrom))
            query = query.Where(l => l.Plant.CompareTo(filter.PlantFrom) >= 0);
        if (!string.IsNullOrEmpty(filter.PlantTo))
            query = query.Where(l => l.Plant.CompareTo(filter.PlantTo) <= 0);
        if (!string.IsNullOrEmpty(filter.InspLotOrigin))
            query = query.Where(l => l.InspectionType == filter.InspLotOrigin);
        if (!string.IsNullOrEmpty(filter.MaterialCodeFrom))
            query = query.Where(l => l.MaterialCode.CompareTo(filter.MaterialCodeFrom) >= 0);
        if (!string.IsNullOrEmpty(filter.MaterialCodeTo))
            query = query.Where(l => l.MaterialCode.CompareTo(filter.MaterialCodeTo) <= 0);
        if (!string.IsNullOrEmpty(filter.BatchNumber))
            query = query.Where(l => l.BatchNumber == filter.BatchNumber);
        if (!string.IsNullOrEmpty(filter.MaterialClass))
            query = query.Where(l => l.MaterialName.Contains(filter.MaterialClass));
        if (!string.IsNullOrEmpty(filter.AssignedInspector ?? filter.RefFieldMonitor))
            query = query.Where(l => l.AssignedInspector == (filter.AssignedInspector ?? filter.RefFieldMonitor));

        var lotNumbersWithUD = _db.UsageDecisions.Select(u => u.LotNumber).Distinct();

        switch (filter.UsageDecisionFilter)
        {
            case UsageDecisionFilter.WithoutUsageDecision:
                query = query.Where(l => !lotNumbersWithUD.Contains(l.LotNumber));
                break;
            case UsageDecisionFilter.WithUsageDecision:
                query = query.Where(l => lotNumbersWithUD.Contains(l.LotNumber));
                break;
        }

        query = query.OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync();

        var lots = await query.Take(filter.MaxHits + 1).ToListAsync();

        var hasMore = lots.Count > filter.MaxHits;
        if (hasMore) lots.RemoveAt(lots.Count - 1);

        var lotNumbers = lots.Select(l => l.LotNumber).ToList();

        var usageDecisions = await _db.UsageDecisions
            .Where(u => lotNumbers.Contains(u.LotNumber))
            .GroupBy(u => u.LotNumber)
            .Select(g => g.OrderByDescending(u => u.DecisionDate).First())
            .ToListAsync();

        var udLookup = usageDecisions.ToDictionary(u => u.LotNumber, u => u);

        var results = lots.Select(lot =>
        {
            var hasUD = udLookup.TryGetValue(lot.LotNumber, out var ud);
            return new InspectionLotSelectionResultDto
            {
                Id = lot.Id,
                LotNumber = lot.LotNumber,
                MaterialCode = lot.MaterialCode,
                MaterialName = lot.MaterialName,
                Plant = lot.Plant,
                BatchNumber = lot.BatchNumber,
                InspectionType = lot.InspectionType,
                InspectionLotOrigin = lot.InspectionType,
                Quantity = lot.Quantity,
                BaseUOM = lot.BaseUOM,
                Status = lot.Status,
                AssignedInspector = lot.AssignedInspector,
                InspectionPlanID = lot.InspectionPlanID,
                ReferenceOrderNumber = lot.ReferenceOrderNumber,
                SampleSize = lot.SampleSize,
                Inspected = lot.Inspected,
                Passed = lot.Passed,
                Failed = lot.Failed,
                CreatedAt = lot.CreatedAt,
                UpdatedAt = lot.UpdatedAt,
                StorageLocation = lot.StorageLocation,
                HasUsageDecision = hasUD,
                UDCode = hasUD ? ud!.UDCode : null,
                UDDecision = hasUD ? ud!.Decision : null,
                DecisionDate = hasUD ? ud!.DecisionDate : null
            };
        }).ToList();

        return new InspectionLotSelectionResponseDto
        {
            Results = results,
            TotalCount = totalCount,
            ReturnedCount = results.Count,
            MaxHits = filter.MaxHits,
            HasMore = hasMore
        };
    }
}
