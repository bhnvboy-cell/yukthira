using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/process")]
public class MesProcessController : MesControllerBase
{
    private readonly MesProcessService _service;

    public MesProcessController(MesProcessService service) => _service = service;

    public record DownloadRecipeRequest(Guid ResourceId);

    [HttpPost("recipes/{recipeId:guid}/download")]
    public async Task<IActionResult> DownloadRecipe(Guid recipeId, [FromBody] DownloadRecipeRequest req, CancellationToken ct)
        => Ok(await _service.DownloadRecipeToResourceAsync(CurrentTenantId, recipeId, req.ResourceId, CurrentActor, ct));

    public record RaiseAlarmRequest(Guid TagId, Guid ResourceId, int Severity, string? Comment);

    [HttpPost("alarms")]
    public async Task<IActionResult> RaiseAlarm([FromBody] RaiseAlarmRequest req, CancellationToken ct)
        => Ok(await _service.RaiseAlarmAsync(CurrentTenantId, CurrentPlantCode, req.TagId, req.ResourceId, req.Severity, req.Comment, ct));

    public record AckAlarmRequest(string? Comment);

    [HttpPost("alarms/{alarmId:guid}/ack")]
    public async Task<IActionResult> AckAlarm(Guid alarmId, [FromBody] AckAlarmRequest? req, CancellationToken ct)
        => Ok(await _service.AcknowledgeAlarmAsync(CurrentTenantId, alarmId, CurrentActor, req?.Comment, ct));

    public record BypassInterlockRequest(int DurationMinutes, Guid EsignId);

    [HttpPost("interlocks/{interlockId:guid}/bypass")]
    public async Task<IActionResult> BypassInterlock(Guid interlockId, [FromBody] BypassInterlockRequest req, CancellationToken ct)
        => Ok(await _service.BypassInterlockAsync(CurrentTenantId, interlockId, CurrentActor, TimeSpan.FromMinutes(req.DurationMinutes), req.EsignId, ct));
}
