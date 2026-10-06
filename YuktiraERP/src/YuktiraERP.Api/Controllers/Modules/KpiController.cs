using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/kpi")]
[Authorize]
public class KpiController : ControllerBase
{
    private readonly IKpiService _kpiService;
    private readonly ITenantContext _tenant;

    public KpiController(IKpiService kpiService, ITenantContext tenant)
    {
        _kpiService = kpiService;
        _tenant = tenant;
    }

    private Guid TenantId => _tenant.TenantId;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardKpis()
        => Ok(await _kpiService.GetDashboardKpisAsync(TenantId));

    [HttpGet("module-badges")]
    public async Task<IActionResult> GetModuleBadges()
        => Ok(await _kpiService.GetModuleKpiBadgesAsync(TenantId));

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
        => Ok(await _kpiService.GetAvailableKpisAsync(TenantId));

    [HttpGet("{code}")]
    public async Task<IActionResult> GetDrillDown(string code)
        => Ok(await _kpiService.GetDrillDownKpiAsync(TenantId, code));

    [HttpGet("{code}/history")]
    public async Task<IActionResult> GetHistory(string code, [FromQuery] int days = 30)
        => Ok(await _kpiService.GetHistoricalKpiSnapshotsAsync(TenantId, code, days));
}
