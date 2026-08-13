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
    [HttpGet("lots/{id:guid}")]
    public async Task<IActionResult> GetLot(Guid id)
    {
        var item = await _lots.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("lots")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateLot([FromBody] InspectionLotEntity model) { model.Id = Guid.NewGuid(); await _lots.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("lots/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateLot(Guid id, [FromBody] InspectionLotEntity model)
    {
        var exists = (await _lots.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _lots.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("lots/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteLot(Guid id)
    {
        var exists = (await _lots.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _lots.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("plans")] public async Task<IActionResult> GetPlans() => Ok(new { data = await _plans.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("plans/{id:guid}")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        var item = await _plans.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("plans")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreatePlan([FromBody] InspectionPlanEntity model) { model.Id = Guid.NewGuid(); await _plans.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("plans/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] InspectionPlanEntity model)
    {
        var exists = (await _plans.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _plans.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("plans/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        var exists = (await _plans.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _plans.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("inspection-results")] public async Task<IActionResult> GetInspectionResults() => Ok(new { data = await _results.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("inspection-results/{id:guid}")]
    public async Task<IActionResult> GetInspectionResult(Guid id)
    {
        var item = await _results.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("inspection-results")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateInspectionResult([FromBody] InspectionResultEntity model) { model.Id = Guid.NewGuid(); await _results.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("inspection-results/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateInspectionResult(Guid id, [FromBody] InspectionResultEntity model)
    {
        var exists = (await _results.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _results.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("inspection-results/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteInspectionResult(Guid id)
    {
        var exists = (await _results.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _results.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("usage-decisions")] public async Task<IActionResult> GetUsageDecisions() => Ok(new { data = await _decisions.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("usage-decisions/{id:guid}")]
    public async Task<IActionResult> GetUsageDecision(Guid id)
    {
        var item = await _decisions.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("usage-decisions")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateUsageDecision([FromBody] UsageDecisionEntity model) { model.Id = Guid.NewGuid(); await _decisions.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("usage-decisions/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateUsageDecision(Guid id, [FromBody] UsageDecisionEntity model)
    {
        var exists = (await _decisions.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _decisions.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("usage-decisions/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteUsageDecision(Guid id)
    {
        var exists = (await _decisions.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _decisions.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
}
