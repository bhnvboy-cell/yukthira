using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesSchedulingService
{
    private readonly MesDbContext _db;

    public MesSchedulingService(MesDbContext db) => _db = db;

    public async Task<MesScheduleVersion> CreateScheduleVersionAsync(
        Guid tenantId, string plantCode, string versionCode,
        DateTime horizonStart, DateTime horizonEnd, string algorithm = "EDD_CPSAT",
        CancellationToken ct = default)
    {
        var version = new MesScheduleVersion
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            VersionCode = versionCode,
            HorizonStart = horizonStart,
            HorizonEnd = horizonEnd,
            Algorithm = algorithm,
            Status = ScheduleVersionStatus.DRAFT
        };
        _db.ScheduleVersions.Add(version);
        await _db.SaveChangesAsync(ct);
        return version;
    }

    /// <summary>
    /// Sequences un-locked operations by priority + due date (EDD) and computes setup/run windows.
    /// Respects planner locks (is_locked = true) per blueprint Section 3.
    /// </summary>
    public async Task<IReadOnlyList<MesScheduleOp>> SequenceOperationsAsync(
        Guid tenantId, Guid versionId, Guid resourceId, IEnumerable<Guid> workOrderIds, CancellationToken ct = default)
    {
        var version = await _db.ScheduleVersions.FirstOrDefaultAsync(v => v.Id == versionId && v.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Schedule version {versionId} not found.");

        if (version.Status == ScheduleVersionStatus.FROZEN)
            throw new InvalidOperationException("Cannot re-sequence a FROZEN schedule version.");

        var lockedOps = await _db.ScheduleOps
            .Where(o => o.TenantId == tenantId && o.VersionId == versionId && o.ResourceId == resourceId && o.IsLocked)
            .OrderBy(o => o.SetupStart)
            .ToListAsync(ct);

        var woIds = workOrderIds.ToList();
        var workOrders = await _db.WorkOrders
            .Where(w => w.TenantId == tenantId && woIds.Contains(w.Id))
            .OrderBy(w => w.Priority)
            .ThenBy(w => w.DueDate)
            .ToListAsync(ct);

        var cursor = version.HorizonStart;
        if (lockedOps.Count > 0)
            cursor = lockedOps.Max(o => o.RunEnd);

        var scheduled = new List<MesScheduleOp>(lockedOps);
        foreach (var wo in workOrders)
        {
            var ops = await _db.WoOperations
                .Where(o => o.TenantId == tenantId && o.WoId == wo.Id)
                .OrderBy(o => o.OperationNo)
                .ToListAsync(ct);

            foreach (var op in ops)
            {
                if (lockedOps.Any(l => l.ProdOrderId == wo.ProdOrderId && l.OperationNo == op.OperationNo))
                    continue;

                var setupStart = cursor;
                var setupEnd = setupStart.AddMinutes(op.StdSetupMin);
                var runStart = setupEnd;
                var runEnd = runStart.AddMinutes(op.StdRunMin * (double)op.QtyPlanned);

                var schedOp = new MesScheduleOp
                {
                    TenantId = tenantId,
                    PlantCode = version.PlantCode,
                    VersionId = versionId,
                    ProdOrderId = wo.ProdOrderId,
                    OperationNo = op.OperationNo,
                    ResourceId = resourceId,
                    SetupStart = setupStart,
                    SetupEnd = setupEnd,
                    RunStart = runStart,
                    RunEnd = runEnd,
                    Priority = wo.Priority,
                    IsLocked = false,
                    SlackMin = (wo.DueDate - runEnd).TotalMinutes
                };
                _db.ScheduleOps.Add(schedOp);
                scheduled.Add(schedOp);
                cursor = runEnd;
            }
        }

        await _db.SaveChangesAsync(ct);
        return scheduled;
    }

    public async Task<MesScheduleVersion> PublishAsync(Guid tenantId, Guid versionId, CancellationToken ct = default)
    {
        var version = await _db.ScheduleVersions.FirstOrDefaultAsync(v => v.Id == versionId && v.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Schedule version {versionId} not found.");
        version.Status = ScheduleVersionStatus.PUBLISHED;
        await _db.SaveChangesAsync(ct);
        return version;
    }
}
