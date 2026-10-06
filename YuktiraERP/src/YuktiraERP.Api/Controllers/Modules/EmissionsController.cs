using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/esg/emissions")]
[Authorize]
public class EmissionsController : ControllerBase
{
    private readonly IEmissionsTrackerService _emissions;
    private readonly ITenantContext _tenant;

    public EmissionsController(IEmissionsTrackerService emissions, ITenantContext tenant)
    {
        _emissions = emissions;
        _tenant = tenant;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string? period, CancellationToken ct)
    {
        var summary = await _emissions.GetSummaryAsync(_tenant.TenantId, period, ct);
        return Ok(new { data = summary, tenantId = _tenant.TenantId });
    }

    [HttpPost("recompute")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> Recompute(CancellationToken ct)
    {
        var result = await _emissions.RecomputeAsync(_tenant.TenantId, User.Identity?.Name ?? "system", ct);
        return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("factors")]
    public async Task<IActionResult> GetFactors([FromQuery] int? scope, [FromQuery] string? sourceType, CancellationToken ct)
    {
        var factors = await _emissions.GetFactorsAsync(_tenant.TenantId, scope, sourceType, ct);
        return Ok(new { data = factors, count = factors.Count, tenantId = _tenant.TenantId });
    }

    [HttpPost("factors")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> UpsertFactor([FromBody] EmissionFactorDto factor, CancellationToken ct)
    {
        if (factor == null)
            return BadRequest(new { success = false, error = "Emission factor payload is required" });
        if (factor.Scope < 1 || factor.Scope > 3)
            return BadRequest(new { success = false, error = "Emission factor scope must be 1, 2 or 3" });
        if (string.IsNullOrWhiteSpace(factor.SourceType))
            return BadRequest(new { success = false, error = "Emission factor source type is required" });

        try
        {
            var saved = await _emissions.UpsertFactorAsync(factor, _tenant.TenantId, User.Identity?.Name ?? "system", ct);
            return Ok(new { success = true, data = saved, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] string? period,
        [FromQuery] int? scope,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _emissions.GetLedgerAsync(_tenant.TenantId, period, scope, page, pageSize);
        return Ok(new
        {
            data = result.Items,
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            tenantId = _tenant.TenantId
        });
    }
}
