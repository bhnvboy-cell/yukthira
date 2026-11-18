using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesLaborService
{
    private readonly MesDbContext _db;

    public MesLaborService(MesDbContext db) => _db = db;

    public async Task<MesLaborClock> ClockInAsync(
        Guid tenantId, string plantCode, Guid employeeId, string terminalId, Guid? shiftId = null, CancellationToken ct = default)
    {
        var clock = new MesLaborClock
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            EmployeeId = employeeId,
            TerminalId = terminalId,
            ShiftId = shiftId,
            ClockIn = DateTime.UtcNow,
            Status = LaborClockStatus.OPEN
        };
        _db.LaborClocks.Add(clock);
        await _db.SaveChangesAsync(ct);
        return clock;
    }

    public async Task<MesLaborClock> ClockOutAsync(Guid tenantId, Guid clockId, CancellationToken ct = default)
    {
        var clock = await _db.LaborClocks.FirstOrDefaultAsync(c => c.Id == clockId && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Labor clock {clockId} not found.");
        clock.ClockOut = DateTime.UtcNow;
        clock.Status = LaborClockStatus.CLOSED;
        await _db.SaveChangesAsync(ct);
        return clock;
    }

    public async Task<MesLaborTaskLog> StartTaskAsync(
        Guid tenantId, string plantCode, Guid employeeId, Guid? woOpId, Guid? resourceId,
        LaborActivityType activityType, bool supervisorOverride = false, CancellationToken ct = default)
    {
        if (resourceId.HasValue)
        {
            var reqs = await _db.OperationSkillReqs
                .Where(r => r.TenantId == tenantId && r.ResourceId == resourceId.Value)
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            foreach (var req in reqs)
            {
                var empSkill = await _db.EmployeeSkills.FirstOrDefaultAsync(es =>
                    es.TenantId == tenantId && es.EmployeeId == employeeId && es.SkillId == req.SkillId, ct);

                var qualified = empSkill != null && empSkill.Level >= req.MinLevel &&
                    (!empSkill.ExpiresOn.HasValue || empSkill.ExpiresOn.Value >= now);

                if (!qualified && !supervisorOverride)
                {
                    _db.LaborExceptions.Add(new MesLaborException
                    {
                        TenantId = tenantId,
                        PlantCode = plantCode,
                        EmployeeId = employeeId,
                        ExceptionType = LaborExceptionType.UNQUALIFIED,
                        Status = "OPEN"
                    });
                    await _db.SaveChangesAsync(ct);
                    throw new InvalidOperationException($"Operator {employeeId} lacks required skill {req.SkillId} (min level {req.MinLevel}).");
                }
            }
        }

        var log = new MesLaborTaskLog
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            EmployeeId = employeeId,
            WoOpId = woOpId,
            ResourceId = resourceId,
            StartTs = DateTime.UtcNow,
            ActivityType = activityType
        };
        _db.LaborTaskLogs.Add(log);
        await _db.SaveChangesAsync(ct);
        return log;
    }
}
