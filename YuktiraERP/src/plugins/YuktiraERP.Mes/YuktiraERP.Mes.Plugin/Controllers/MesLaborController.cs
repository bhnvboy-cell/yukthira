using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/labor")]
public class MesLaborController : MesControllerBase
{
    private readonly MesLaborService _service;

    public MesLaborController(MesLaborService service) => _service = service;

    public record ClockInRequest(Guid EmployeeId, string TerminalId, Guid? ShiftId);

    [HttpPost("clock-in")]
    public async Task<IActionResult> ClockIn([FromBody] ClockInRequest req, CancellationToken ct)
        => Ok(await _service.ClockInAsync(CurrentTenantId, CurrentPlantCode, req.EmployeeId, req.TerminalId, req.ShiftId, ct));

    [HttpPost("clock-out/{clockId:guid}")]
    public async Task<IActionResult> ClockOut(Guid clockId, CancellationToken ct)
        => Ok(await _service.ClockOutAsync(CurrentTenantId, clockId, ct));

    public record StartTaskRequest(Guid EmployeeId, Guid? WoOpId, Guid? ResourceId, LaborActivityType ActivityType, bool SupervisorOverride = false);

    [HttpPost("tasks/start")]
    public async Task<IActionResult> StartTask([FromBody] StartTaskRequest req, CancellationToken ct)
        => Ok(await _service.StartTaskAsync(
            CurrentTenantId, CurrentPlantCode, req.EmployeeId, req.WoOpId, req.ResourceId, req.ActivityType, req.SupervisorOverride, ct));
}
