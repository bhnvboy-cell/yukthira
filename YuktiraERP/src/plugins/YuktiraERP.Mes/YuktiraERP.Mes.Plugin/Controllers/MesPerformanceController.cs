using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/performance")]
public class MesPerformanceController : MesControllerBase
{
    private readonly MesOeeService _service;

    public MesPerformanceController(MesOeeService service) => _service = service;

    [HttpGet("oee")]
    public async Task<IActionResult> GetOee([FromQuery] Guid? resourceId, [FromQuery] int take = 60, CancellationToken ct = default)
        => Ok(await _service.GetIntervalsAsync(CurrentTenantId, resourceId, take, ct));

    public record RecordOeeRequest(
        Guid ResourceId, DateTime IntervalStart, OeeIntervalType IntervalType,
        double PlannedTimeS, double RunTimeS, double IdealCycleS,
        long TotalCount, long GoodCount, bool IsDataGapped = false);

    [HttpPost("oee")]
    public async Task<IActionResult> RecordOee([FromBody] RecordOeeRequest req, CancellationToken ct)
    {
        var interval = await _service.RecordIntervalAsync(
            CurrentTenantId, CurrentPlantCode, req.ResourceId, req.IntervalStart, req.IntervalType,
            req.PlannedTimeS, req.RunTimeS, req.IdealCycleS, req.TotalCount, req.GoodCount, req.IsDataGapped, ct);
        return Ok(interval);
    }
}
