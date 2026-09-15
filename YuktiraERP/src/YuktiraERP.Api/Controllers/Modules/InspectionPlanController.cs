using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/qm/inspection-plans")]
[Authorize]
public class InspectionPlanController : ControllerBase
{
    private readonly IInspectionPlanService _planService;
    private readonly IAutoInspectionPlanGenerator _autoGenerator;

    public InspectionPlanController(
        IInspectionPlanService planService,
        IAutoInspectionPlanGenerator autoGenerator)
    {
        _planService = planService;
        _autoGenerator = autoGenerator;
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("TenantId");
        return claim != null && Guid.TryParse(claim.Value, out var tid) ? tid : Guid.Empty;
    }

    [HttpPost("generic")]
    public async Task<IActionResult> CreateGenericPlan([FromBody] InspectionPlanCreateRequest request)
    {
        var result = await _planService.CreateGenericPlanAsync(request, GetTenantId());
        return Ok(result);
    }

    [HttpPost("{planId}/operations/characteristics")]
    public async Task<IActionResult> AssignMic(Guid planId, [FromBody] InspectionPlanMicAssignRequest request)
    {
        try
        {
            var result = await _planService.AssignMicToOperationAsync(planId, request, GetTenantId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("resolve")]
    public async Task<IActionResult> ResolvePlan([FromBody] InspectionPlanResolveRequest request)
    {
        var result = await _planService.ResolvePlanAsync(request, GetTenantId());
        return result != null ? Ok(result) : NotFound(new { message = "No matching inspection plan found." });
    }

    [HttpPost("auto-generate")]
    public async Task<IActionResult> AutoGenerate([FromBody] AutoGenerationRequest request)
    {
        try
        {
            var result = await _autoGenerator.GeneratePlanAsync(request, GetTenantId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{planId}")]
    public async Task<IActionResult> GetPlan(Guid planId)
    {
        var result = await _planService.GetPlanAsync(planId, GetTenantId());
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet]
    public async Task<IActionResult> GetPlans([FromQuery] string? plantId = null, [FromQuery] string? materialId = null)
    {
        var result = await _planService.GetPlansAsync(GetTenantId(), plantId, materialId);
        return Ok(result);
    }
}
