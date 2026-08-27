using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/mm/movement-types")]
[Authorize]
public class MovementTypeController : ControllerBase
{
    private readonly YuktiraERP.Core.Interfaces.IMovementTypeEngineService _engineService;
    private readonly ITenantContext _tenant;

    public MovementTypeController(YuktiraERP.Core.Interfaces.IMovementTypeEngineService engineService, ITenantContext tenant)
    {
        _engineService = engineService;
        _tenant = tenant;
    }

    private Guid ResolveTenantId()
    {
        if (_tenant != null && _tenant.TenantId != Guid.Empty) return _tenant.TenantId;
        var claim = User?.FindFirst("TenantId")?.Value ?? User?.FindFirst("tenantId")?.Value;
        if (Guid.TryParse(claim, out var tid)) return tid;
        return _tenant?.TenantId ?? Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tid = ResolveTenantId();
        var types = await _engineService.GetAllMovementTypesAsync(tid);
        return Ok(new { data = types, tenantId = tid });
    }

    [HttpGet("{mvt:int}")]
    public async Task<IActionResult> GetById(int mvt)
    {
        var tid = ResolveTenantId();
        var type = await _engineService.GetMovementTypeAsync(mvt, tid);
        if (type == null) return NotFound(new { error = $"Movement type {mvt} not found" });
        return Ok(new { data = type, tenantId = tid });
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var tid = ResolveTenantId();
        var types = await _engineService.GetByCategoryAsync(category, tid);
        return Ok(new { data = types, tenantId = tid });
    }

    [HttpGet("stock-type/{stockType}")]
    public async Task<IActionResult> GetByStockType(string stockType)
    {
        var tid = ResolveTenantId();
        var types = await _engineService.GetByStockTypeAsync(stockType, tid);
        return Ok(new { data = types, tenantId = tid });
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateMovementAsync([FromBody] MovementValidationRequest request)
    {
        request.TenantId = ResolveTenantId();
        var result = await _engineService.ValidateMovementAsync(request);
        return Ok(new { data = result, tenantId = request.TenantId });
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> SimulateWorkflowAsync([FromBody] MovementSimulationRequest request)
    {
        request.TenantId = ResolveTenantId();
        var result = await _engineService.SimulateWorkflowAsync(request);
        return Ok(new { data = result, tenantId = request.TenantId });
    }

    [HttpPost("post")]
    public async Task<IActionResult> PostMovementAsync([FromBody] MovementPostRequest request)
    {
        request.TenantId = ResolveTenantId();
        var result = await _engineService.PostMovementAsync(request);
        if (!result.Success)
            return BadRequest(new { errors = result.Errors, tenantId = request.TenantId });
        return Ok(new { data = result, tenantId = request.TenantId });
    }

    [HttpPost("{documentId:guid}/reverse")]
    public async Task<IActionResult> ReverseMovementAsync(Guid documentId, [FromBody] ReverseRequest request)
    {
        var tid = ResolveTenantId();
        var result = await _engineService.ReverseMovementAsync(documentId, request.Reason, request.UserId);
        if (!result.Success)
            return BadRequest(new { errors = result.Errors, tenantId = tid });
        return Ok(new { data = result, tenantId = tid });
    }

    [HttpGet("{documentId:guid}/trace")]
    public async Task<IActionResult> GetMovementTraceAsync(Guid documentId)
    {
        var tid = ResolveTenantId();
        var trace = await _engineService.GetMovementTraceAsync(documentId);
        return Ok(new { data = trace, tenantId = tid });
    }

    [HttpGet("document-flow")]
    public async Task<IActionResult> GetDocumentFlowAsync([FromQuery] string reference, [FromQuery] string referenceType)
    {
        var tid = ResolveTenantId();
        var flow = await _engineService.GetDocumentFlowAsync(reference, referenceType, tid);
        return Ok(new { data = flow, tenantId = tid });
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetAllCategoriesAsync()
    {
        var tid = ResolveTenantId();
        var categories = await _engineService.GetAllCategoriesAsync(tid);
        return Ok(new { data = categories, tenantId = tid });
    }

    [HttpGet("stock-types")]
    public async Task<IActionResult> GetAllStockTypesAsync()
    {
        var tid = ResolveTenantId();
        var stockTypes = await _engineService.GetAllStockTypesAsync(tid);
        return Ok(new { data = stockTypes, tenantId = tid });
    }
}

public class ReverseRequest
{
    public string Reason { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}
