using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Core.Services;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesQualityService
{
    private readonly MesDbContext _db;

    public MesQualityService(MesDbContext db) => _db = db;

    public async Task<MesQcResult> RecordQcResultAsync(
        Guid tenantId, string plantCode, Guid taskId, Guid characteristicId,
        int sampleNo, double value, string uom, double lowLimit, double highLimit,
        Guid? unitId = null, Guid? lotId = null, CancellationToken ct = default)
    {
        var inTolerance = value >= lowLimit && value <= highLimit;
        var result = new MesQcResult
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            TaskId = taskId,
            CharacteristicId = characteristicId,
            SampleNo = sampleNo,
            Value = value,
            Uom = uom,
            InTolerance = inTolerance,
            UnitId = unitId,
            MeasuredAt = DateTime.UtcNow
        };
        _db.QcResults.Add(result);

        // Auto-create NC & quarantine lot if out of tolerance (Section 8)
        if (!inTolerance)
        {
            var nc = new MesNonConformance
            {
                TenantId = tenantId,
                PlantCode = plantCode,
                NcCode = $"NC-{DateTime.UtcNow:yyyyMMddHHmmss}-{sampleNo}",
                DefectCode = "OUT_OF_TOLERANCE",
                Severity = "MAJOR",
                Qty = 1,
                UnitId = unitId,
                LotId = lotId,
                Disposition = NcDisposition.REWORK,
                Status = "OPEN"
            };
            _db.NonConformances.Add(nc);

            if (lotId.HasValue)
            {
                await PlaceHoldInternalAsync(tenantId, plantCode, HoldTargetType.LOT, lotId.Value,
                    $"Auto-hold: out-of-tolerance QC sample ({value} {uom})", "SYSTEM", ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<MesSpcViolation>> EvaluateSpcSubgroupAsync(
        Guid tenantId, Guid chartId, IReadOnlyList<double> subgroupSamples, CancellationToken ct = default)
    {
        var chart = await _db.SpcCharts.FirstOrDefaultAsync(c => c.Id == chartId && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"SPC chart {chartId} not found.");

        if (subgroupSamples.Count == 0) return Array.Empty<MesSpcViolation>();

        var mean = subgroupSamples.Average();
        var range = subgroupSamples.Max() - subgroupSamples.Min();
        var stdDev = subgroupSamples.Count > 1
            ? Math.Sqrt(subgroupSamples.Sum(x => Math.Pow(x - mean, 2)) / (subgroupSamples.Count - 1))
            : 0;

        var point = new MesSpcPoint
        {
            TenantId = tenantId,
            PlantCode = chart.PlantCode,
            ChartId = chartId,
            Mean = mean,
            Range = range,
            StdDev = stdDev,
            SampleCount = subgroupSamples.Count,
            RecordedAt = DateTime.UtcNow
        };
        _db.SpcPoints.Add(point);

        var history = await _db.SpcPoints
            .Where(p => p.TenantId == tenantId && p.ChartId == chartId)
            .OrderByDescending(p => p.RecordedAt)
            .Take(7)
            .Select(p => p.Mean)
            .ToListAsync(ct);
        history.Reverse();
        history.Add(mean);

        var violatedRules = SpcRuleEngine.EvaluateWesternElectric(history, chart.Cl, chart.Ucl, chart.Lcl);
        var violations = new List<MesSpcViolation>();
        foreach (var ruleId in violatedRules)
        {
            var v = new MesSpcViolation
            {
                TenantId = tenantId,
                PlantCode = chart.PlantCode,
                ChartId = chartId,
                PointId = point.Id,
                RuleId = ruleId,
                DetectedAt = DateTime.UtcNow,
                ActionTaken = "ALERT"
            };
            _db.SpcViolations.Add(v);
            violations.Add(v);
        }

        await _db.SaveChangesAsync(ct);
        return violations;
    }

    public async Task<MesHold> PlaceHoldAsync(
        Guid tenantId, string plantCode, HoldTargetType targetType, Guid targetId,
        string reason, string placedBy, CancellationToken ct = default)
    {
        var hold = await PlaceHoldInternalAsync(tenantId, plantCode, targetType, targetId, reason, placedBy, ct);
        await _db.SaveChangesAsync(ct);
        return hold;
    }

    private Task<MesHold> PlaceHoldInternalAsync(
        Guid tenantId, string plantCode, HoldTargetType targetType, Guid targetId,
        string reason, string placedBy, CancellationToken ct)
    {
        var hold = new MesHold
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason,
            PlacedBy = placedBy,
            PlacedAt = DateTime.UtcNow,
            Status = HoldStatus.ACTIVE
        };
        hold.RecordHash = RecordHashHelper.ComputeSha256(hold.Id, hold.TargetType, hold.TargetId, hold.Reason, hold.PlacedBy, hold.PlacedAt);
        _db.Holds.Add(hold);
        return Task.FromResult(hold);
    }
}
