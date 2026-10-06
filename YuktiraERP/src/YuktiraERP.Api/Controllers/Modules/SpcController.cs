using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/qm/spc")]
[Authorize]
public class SpcController : ControllerBase
{
    private readonly ISpcEngineService _spc;
    private readonly ITenantContext _tenant;

    public SpcController(ISpcEngineService spc, ITenantContext tenant)
    {
        _spc = spc;
        _tenant = tenant;
    }

    /// <summary>SPC analysis (X̄/R/S charts, capability, run-rule violations).</summary>
    [HttpGet("analysis")]
    public async Task<IActionResult> GetAnalysis([FromQuery] SpcQueryDto query, CancellationToken ct)
    {
        var result = await _spc.AnalyzeAsync(query ?? new SpcQueryDto(), ct);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    /// <summary>Distinct characteristics / material codes / plants for the filter bar.</summary>
    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters(CancellationToken ct)
    {
        var result = await _spc.GetFilterOptionsAsync(ct);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }
}
