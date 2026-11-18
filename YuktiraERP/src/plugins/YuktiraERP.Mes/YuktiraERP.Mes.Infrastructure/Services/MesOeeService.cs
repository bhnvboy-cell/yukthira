using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Core.Services;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesOeeService
{
    private readonly MesDbContext _db;

    public MesOeeService(MesDbContext db) => _db = db;

    public async Task<MesOeeInterval> RecordIntervalAsync(
        Guid tenantId, string plantCode, Guid resourceId, DateTime intervalStart,
        OeeIntervalType intervalType, double plannedTimeS, double runTimeS,
        double idealCycleS, long totalCount, long goodCount, bool isDataGapped = false,
        CancellationToken ct = default)
    {
        var interval = OeeCalculator.ComputeInterval(
            tenantId, plantCode, resourceId, intervalStart, intervalType,
            plannedTimeS, runTimeS, idealCycleS, totalCount, goodCount, isDataGapped);

        _db.OeeIntervals.Add(interval);
        await _db.SaveChangesAsync(ct);
        return interval;
    }

    public async Task<IReadOnlyList<MesOeeInterval>> GetIntervalsAsync(
        Guid tenantId, Guid? resourceId = null, int take = 60, CancellationToken ct = default)
    {
        var q = _db.OeeIntervals.Where(o => o.TenantId == tenantId && !o.IsDataGapped);
        if (resourceId.HasValue)
            q = q.Where(o => o.ResourceId == resourceId.Value);

        return await q.OrderByDescending(o => o.IntervalStart).Take(take).ToListAsync(ct);
    }
}
