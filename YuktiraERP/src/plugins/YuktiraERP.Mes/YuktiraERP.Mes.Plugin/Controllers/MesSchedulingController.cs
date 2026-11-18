using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/scheduling")]
public class MesSchedulingController : MesControllerBase
{
    private readonly MesSchedulingService _service;

    public MesSchedulingController(MesSchedulingService service) => _service = service;

    public record CreateVersionRequest(string VersionCode, DateTime HorizonStart, DateTime HorizonEnd, string Algorithm = "EDD_CPSAT");

    [HttpPost("versions")]
    public async Task<IActionResult> CreateVersion([FromBody] CreateVersionRequest req, CancellationToken ct)
        => Ok(await _service.CreateScheduleVersionAsync(CurrentTenantId, CurrentPlantCode, req.VersionCode, req.HorizonStart, req.HorizonEnd, req.Algorithm, ct));

    public record SequenceRequest(Guid ResourceId, List<Guid> WorkOrderIds);

    [HttpPost("versions/{versionId:guid}/sequence")]
    public async Task<IActionResult> Sequence(Guid versionId, [FromBody] SequenceRequest req, CancellationToken ct)
        => Ok(await _service.SequenceOperationsAsync(CurrentTenantId, versionId, req.ResourceId, req.WorkOrderIds, ct));

    [HttpPost("versions/{versionId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid versionId, CancellationToken ct)
        => Ok(await _service.PublishAsync(CurrentTenantId, versionId, ct));
}
