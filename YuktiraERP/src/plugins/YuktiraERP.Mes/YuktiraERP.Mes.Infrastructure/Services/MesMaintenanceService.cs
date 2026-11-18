using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesMaintenanceService
{
    private readonly MesDbContext _db;

    public MesMaintenanceService(MesDbContext db) => _db = db;

    public async Task<(MesEquipmentMeter Meter, bool PmDue)> UpdateMeterAsync(
        Guid tenantId, Guid equipmentId, MeterType meterType, double newCounter, CancellationToken ct = default)
    {
        var meter = await _db.EquipmentMeters
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.EquipmentId == equipmentId && m.MeterType == meterType, ct);

        if (meter == null)
        {
            meter = new MesEquipmentMeter
            {
                TenantId = tenantId,
                EquipmentId = equipmentId,
                MeterType = meterType,
                Counter = newCounter,
                LastReadAt = DateTime.UtcNow
            };
            _db.EquipmentMeters.Add(meter);
        }
        else
        {
            meter.Counter = newCounter;
            meter.LastReadAt = DateTime.UtcNow;
        }

        var trigger = await _db.PmTriggers.FirstOrDefaultAsync(t =>
            t.TenantId == tenantId && t.EquipmentId == equipmentId && t.TriggerType == PmTriggerType.METER, ct);

        var pmDue = trigger != null && (meter.Counter - meter.LastPmCounter) >= trigger.Threshold;
        if (pmDue)
        {
            _db.OutboxEvents.Add(new MesOutboxEvent
            {
                TenantId = tenantId,
                PlantCode = meter.PlantCode,
                EventType = "pm.trigger.due",
                AggregateType = nameof(MesEquipmentMeter),
                AggregateId = equipmentId,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { equipmentId, meter.Counter, trigger!.Threshold })
            });
        }

        await _db.SaveChangesAsync(ct);
        return (meter, pmDue);
    }

    public async Task<MesAndonCall> RaiseAndonAsync(
        Guid tenantId, string plantCode, Guid lineId, AndonCallType callType, string raisedBy, CancellationToken ct = default)
    {
        var call = new MesAndonCall
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            LineId = lineId,
            CallType = callType,
            RaisedAt = DateTime.UtcNow,
            RaisedBy = raisedBy
        };
        _db.AndonCalls.Add(call);
        await _db.SaveChangesAsync(ct);
        return call;
    }

    public async Task<MesLoto> IssueLotoPermitAsync(
        Guid tenantId, string plantCode, Guid equipmentId, string locksJson, string issuedBy, CancellationToken ct = default)
    {
        var loto = new MesLoto
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            EquipmentId = equipmentId,
            LocksJson = locksJson,
            IssuedBy = issuedBy
        };
        _db.Lotos.Add(loto);
        await _db.SaveChangesAsync(ct);
        return loto;
    }

    public async Task<bool> HasActiveLotoPermitAsync(Guid tenantId, Guid equipmentId, CancellationToken ct = default)
        => await _db.Lotos.AnyAsync(l => l.TenantId == tenantId && l.EquipmentId == equipmentId && l.ClearedAt == null, ct);
}
