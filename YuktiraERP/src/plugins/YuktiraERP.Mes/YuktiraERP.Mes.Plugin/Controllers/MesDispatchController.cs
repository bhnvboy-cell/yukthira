using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/dispatch")]
public class MesDispatchController : MesControllerBase
{
    private readonly MesDispatchService _service;

    public MesDispatchController(MesDispatchService service) => _service = service;

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue([FromQuery] Guid? resourceId, CancellationToken ct)
        => Ok(await _service.GetDispatchQueueAsync(CurrentTenantId, resourceId, ct));

    public record CreateWorkOrderRequest(string WorkOrderNumber, Guid ProdOrderId, Guid MaterialId, decimal PlannedQty, DateTime DueDate, int Priority = 100);

    [HttpPost("work-orders")]
    public async Task<IActionResult> CreateWorkOrder([FromBody] CreateWorkOrderRequest req, CancellationToken ct)
    {
        var wo = await _service.CreateWorkOrderAsync(
            CurrentTenantId, CurrentPlantCode, req.WorkOrderNumber, req.ProdOrderId, req.MaterialId, req.PlannedQty, req.DueDate, req.Priority, ct);
        return Ok(wo);
    }

    public record StartOperationRequest(bool SupervisorOverride = false);

    [HttpPost("operations/{woOpId:guid}/start")]
    public async Task<IActionResult> StartOperation(Guid woOpId, [FromBody] StartOperationRequest? req, CancellationToken ct)
    {
        var op = await _service.StartOperationAsync(CurrentTenantId, woOpId, CurrentActor, req?.SupervisorOverride ?? false, ct);
        return Ok(op);
    }

    public record ConfirmOperationRequest(
        Guid IdempotencyKey, decimal GoodQty, decimal ScrapQty, decimal ReworkQty,
        string? ScrapReason, double LaborMin, double MachineMin);

    [HttpPost("operations/{woOpId:guid}/confirm")]
    public async Task<IActionResult> ConfirmOperation(Guid woOpId, [FromBody] ConfirmOperationRequest req, CancellationToken ct)
    {
        var conf = await _service.ConfirmOperationAsync(
            CurrentTenantId, woOpId, req.IdempotencyKey, req.GoodQty, req.ScrapQty, req.ReworkQty,
            req.ScrapReason, req.LaborMin, req.MachineMin, CurrentActor, ct);
        return Ok(conf);
    }
}
