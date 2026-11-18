using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/maintenance")]
public class MesMaintenanceController : MesControllerBase
{
    private readonly MesMaintenanceService _service;

    public MesMaintenanceController(MesMaintenanceService service) => _service = service;

    public record MeterUpdateRequest(Guid EquipmentId, MeterType MeterType, double Counter);

    [HttpPost("meters")]
    public async Task<IActionResult> UpdateMeter([FromBody] MeterUpdateRequest req, CancellationToken ct)
    {
        var (meter, pmDue) = await _service.UpdateMeterAsync(CurrentTenantId, req.EquipmentId, req.MeterType, req.Counter, ct);
        return Ok(new { meter, pmDue });
    }

    public record AndonRequest(Guid LineId, AndonCallType CallType);

    [HttpPost("andon")]
    public async Task<IActionResult> RaiseAndon([FromBody] AndonRequest req, CancellationToken ct)
        => Ok(await _service.RaiseAndonAsync(CurrentTenantId, CurrentPlantCode, req.LineId, req.CallType, CurrentActor, ct));

    public record LotoRequest(Guid EquipmentId, string LocksJson);

    [HttpPost("loto")]
    public async Task<IActionResult> IssueLoto([FromBody] LotoRequest req, CancellationToken ct)
        => Ok(await _service.IssueLotoPermitAsync(CurrentTenantId, CurrentPlantCode, req.EquipmentId, req.LocksJson, CurrentActor, ct));
}
