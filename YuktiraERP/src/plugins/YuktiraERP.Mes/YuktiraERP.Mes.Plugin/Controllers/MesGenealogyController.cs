using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/genealogy")]
public class MesGenealogyController : MesControllerBase
{
    private readonly MesGenealogyService _service;

    public MesGenealogyController(MesGenealogyService service) => _service = service;

    public record EdgeRequest(string ParentType, Guid ParentId, string ChildType, Guid ChildId, decimal Qty, GenealogyEdgeType EdgeType, Guid? WoOpId, Guid? ResourceId);

    [HttpPost("edges")]
    public async Task<IActionResult> RecordEdge([FromBody] EdgeRequest req, CancellationToken ct)
    {
        var edge = await _service.RecordEdgeAsync(
            CurrentTenantId, CurrentPlantCode, req.ParentType, req.ParentId, req.ChildType, req.ChildId,
            req.Qty, req.EdgeType, req.WoOpId, req.ResourceId, ct);
        return Ok(edge);
    }

    [HttpGet("forward/{id:guid}")]
    public async Task<IActionResult> TraceForward(Guid id, CancellationToken ct)
        => Ok(await _service.TraceForwardDescendantsAsync(CurrentTenantId, id, ct));

    [HttpGet("backward/{id:guid}")]
    public async Task<IActionResult> TraceBackward(Guid id, CancellationToken ct)
        => Ok(await _service.TraceBackwardAncestorsAsync(CurrentTenantId, id, ct));
}
