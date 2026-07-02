using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();
        var widgets = await _dashboardService.GetUserDashboardAsync(userId, tenantId);
        return Ok(widgets);
    }

    [HttpPost("layout")]
    public async Task<IActionResult> SaveLayout([FromBody] List<DashboardWidgetDto> widgets)
    {
        var userId = GetUserId();
        var result = await _dashboardService.SaveWidgetLayoutAsync(userId, widgets);
        return Ok(new { success = result });
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
    {
        var tenantId = GetTenantId();
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "READ_ONLY";
        var widgets = await _dashboardService.GetAvailableWidgetsAsync(tenantId, role);
        return Ok(widgets);
    }

    private Guid GetUserId() =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
}
