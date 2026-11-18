using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/resources")]
public class MesResourcesController : MesControllerBase
{
    private readonly MesResourceService _service;

    public MesResourcesController(MesResourceService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? plantCode, CancellationToken ct)
        => Ok(await _service.GetResourcesAsync(CurrentTenantId, plantCode, ct));

    public record CreateResourceRequest(string ResourceCode, string Name, ResourceType ResourceType, string ErpRefType, Guid? ErpRefId);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResourceRequest req, CancellationToken ct)
    {
        var res = await _service.CreateResourceAsync(
            CurrentTenantId, CurrentPlantCode, req.ResourceCode, req.Name, req.ResourceType, req.ErpRefType, req.ErpRefId, ct);
        return Ok(res);
    }

    public record TransitionStateRequest(ResourceState ToState, string? ReasonCode, StateChangeSource Source);

    [HttpPost("{resourceId:guid}/state")]
    public async Task<IActionResult> TransitionState(Guid resourceId, [FromBody] TransitionStateRequest req, CancellationToken ct)
    {
        var log = await _service.TransitionStateAsync(
            CurrentTenantId, resourceId, req.ToState, req.ReasonCode, req.Source, CurrentActor, ct);
        return Ok(log);
    }

    [HttpGet("alternates")]
    public async Task<IActionResult> Alternates([FromQuery] string capabilityCode, [FromQuery] Guid excludeResourceId, CancellationToken ct)
        => Ok(await _service.FindAlternateResourcesAsync(CurrentTenantId, capabilityCode, excludeResourceId, ct));
}
