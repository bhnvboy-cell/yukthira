using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Controllers;

[ApiController]
[Route("api/kpi")]
[Authorize]
public class KpiController : ControllerBase
{
    private readonly IKpiService _kpiService;

    public KpiController(IKpiService kpiService) => _kpiService = kpiService;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardKpis()
    {
        var tenantId = HttpContext.Request.Query.TryGetValue("tenantId", out var tid)
            ? Guid.Parse(tid.ToString())
            : Guid.Empty;
        var kpis = await _kpiService.GetDashboardKpisAsync(tenantId);
        return Ok(kpis);
    }

    [HttpGet("module-badges")]
    public async Task<IActionResult> GetModuleBadges()
    {
        var tenantId = HttpContext.Request.Query.TryGetValue("tenantId", out var tid)
            ? Guid.Parse(tid.ToString())
            : Guid.Empty;
        var badges = await _kpiService.GetModuleKpiBadgesAsync(tenantId);
        return Ok(badges);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetDrillDown(string code)
    {
        var tenantId = HttpContext.Request.Query.TryGetValue("tenantId", out var tid)
            ? Guid.Parse(tid.ToString())
            : Guid.Empty;
        var kpi = await _kpiService.GetDrillDownKpiAsync(tenantId, code);
        return Ok(kpi);
    }

    [HttpGet("{code}/history")]
    public async Task<IActionResult> GetHistory(string code, [FromQuery] int days = 30)
    {
        var tenantId = HttpContext.Request.Query.TryGetValue("tenantId", out var tid)
            ? Guid.Parse(tid.ToString())
            : Guid.Empty;
        var history = await _kpiService.GetHistoricalKpiSnapshotsAsync(tenantId, code, days);
        return Ok(history);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
    {
        var tenantId = HttpContext.Request.Query.TryGetValue("tenantId", out var tid)
            ? Guid.Parse(tid.ToString())
            : Guid.Empty;
        var kpis = await _kpiService.GetAvailableKpisAsync(tenantId);
        return Ok(kpis);
    }
}
