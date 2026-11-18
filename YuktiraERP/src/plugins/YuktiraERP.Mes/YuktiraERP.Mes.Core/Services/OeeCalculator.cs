using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Services;

public static class OeeCalculator
{
    /// <summary>
    /// Computes Availability, Performance, Quality, and OEE for an interval.
    /// Per blueprint Section 12: time in a data-gapped state is excluded, not treated as downtime.
    /// </summary>
    public static MesOeeInterval ComputeInterval(
        Guid tenantId,
        string plantCode,
        Guid resourceId,
        DateTime intervalStart,
        OeeIntervalType intervalType,
        double plannedTimeS,
        double runTimeS,
        double idealCycleS,
        long totalCount,
        long goodCount,
        bool isDataGapped = false)
    {
        if (isDataGapped)
        {
            return new MesOeeInterval
            {
                TenantId = tenantId,
                PlantCode = plantCode,
                ResourceId = resourceId,
                IntervalStart = intervalStart,
                IntervalType = intervalType,
                PlannedTimeS = plannedTimeS,
                RunTimeS = runTimeS,
                IdealCycleS = idealCycleS,
                TotalCount = totalCount,
                GoodCount = goodCount,
                IsDataGapped = true
            };
        }

        var availability = plannedTimeS > 0 ? Math.Clamp(runTimeS / plannedTimeS, 0d, 1d) : 0d;
        var performance = runTimeS > 0 && idealCycleS > 0
            ? Math.Clamp((idealCycleS * totalCount) / runTimeS, 0d, 1.5d) // capped at 150% for sanity
            : 0d;
        var quality = totalCount > 0 ? Math.Clamp((double)goodCount / totalCount, 0d, 1d) : 0d;
        var oee = availability * performance * quality;

        return new MesOeeInterval
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            ResourceId = resourceId,
            IntervalStart = intervalStart,
            IntervalType = intervalType,
            PlannedTimeS = plannedTimeS,
            RunTimeS = runTimeS,
            IdealCycleS = idealCycleS,
            TotalCount = totalCount,
            GoodCount = goodCount,
            Availability = Math.Round(availability, 4),
            Performance = Math.Round(performance, 4),
            Quality = Math.Round(quality, 4),
            Oee = Math.Round(oee, 4),
            IsDataGapped = false
        };
    }
}
