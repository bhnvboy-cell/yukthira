using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/quality")]
public class MesQualityController : MesControllerBase
{
    private readonly MesQualityService _service;

    public MesQualityController(MesQualityService service) => _service = service;

    public record QcResultRequest(Guid TaskId, Guid CharacteristicId, int SampleNo, double Value, string Uom, double LowLimit, double HighLimit, Guid? UnitId, Guid? LotId);

    [HttpPost("results")]
    public async Task<IActionResult> RecordResult([FromBody] QcResultRequest req, CancellationToken ct)
    {
        var result = await _service.RecordQcResultAsync(
            CurrentTenantId, CurrentPlantCode, req.TaskId, req.CharacteristicId, req.SampleNo,
            req.Value, req.Uom, req.LowLimit, req.HighLimit, req.UnitId, req.LotId, ct);
        return Ok(result);
    }

    public record SpcEvaluateRequest(Guid ChartId, List<double> Samples);

    [HttpPost("spc/evaluate")]
    public async Task<IActionResult> EvaluateSpc([FromBody] SpcEvaluateRequest req, CancellationToken ct)
    {
        var violations = await _service.EvaluateSpcSubgroupAsync(CurrentTenantId, req.ChartId, req.Samples, ct);
        return Ok(violations);
    }

    public record PlaceHoldRequest(HoldTargetType TargetType, Guid TargetId, string Reason);

    [HttpPost("holds")]
    public async Task<IActionResult> PlaceHold([FromBody] PlaceHoldRequest req, CancellationToken ct)
    {
        var hold = await _service.PlaceHoldAsync(CurrentTenantId, CurrentPlantCode, req.TargetType, req.TargetId, req.Reason, CurrentActor, ct);
        return Ok(hold);
    }
}
