using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/qm")]
[Authorize]
public class QualityController : ControllerBase
{
    private readonly IRepository<InspectionLotEntity, Guid> _lots;
    private readonly IRepository<InspectionPlanEntity, Guid> _plans;
    private readonly IRepository<InspectionResultEntity, Guid> _results;
    private readonly IRepository<UsageDecisionEntity, Guid> _decisions;
    private readonly ITenantContext _tenant;

    public QualityController(
        IRepository<InspectionLotEntity, Guid> lots,
        IRepository<InspectionPlanEntity, Guid> plans,
        IRepository<InspectionResultEntity, Guid> results,
        IRepository<UsageDecisionEntity, Guid> decisions,
        ITenantContext tenant)
    {
        _lots = lots;
        _plans = plans;
        _results = results;
        _decisions = decisions;
        _tenant = tenant;
    }

    [HttpGet("lots")] public async Task<IActionResult> GetLots() => Ok(new { data = await _lots.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("lots")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateLot([FromBody] InspectionLotEntity model) { model.Id = Guid.NewGuid(); await _lots.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("plans")] public async Task<IActionResult> GetPlans() => Ok(new { data = await _plans.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("plans")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreatePlan([FromBody] InspectionPlanEntity model) { model.Id = Guid.NewGuid(); await _plans.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("inspection-results")] public async Task<IActionResult> GetInspectionResults() => Ok(new { data = await _results.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("inspection-results")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateInspectionResult([FromBody] InspectionResultEntity model) { model.Id = Guid.NewGuid(); await _results.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("usage-decisions")] public async Task<IActionResult> GetUsageDecisions() => Ok(new { data = await _decisions.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("usage-decisions")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateUsageDecision([FromBody] UsageDecisionEntity model) { model.Id = Guid.NewGuid(); await _decisions.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
}
