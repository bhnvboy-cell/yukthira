using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MRPController : ControllerBase
{
    private readonly IMrpService _mrpService;

    public MRPController(IMrpService mrpService) => _mrpService = mrpService;

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromQuery] Guid? materialId = null)
    {
        var tenantId = GetTenantId();
        var results = await _mrpService.RunMrpAsync(tenantId, materialId);
        return Ok(results);
    }

    [HttpGet("shortages")]
    public async Task<IActionResult> GetShortages()
    {
        var tenantId = GetTenantId();
        var shortages = await _mrpService.GetShortageAlertsAsync(tenantId);
        return Ok(shortages);
    }

    [HttpPost("refresh")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> RefreshViews()
    {
        await _mrpService.RefreshStockViewAsync();
        return Ok(new { message = "Materialized views refreshed" });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetRunHistory([FromQuery] int limit = 20)
    {
        var tenantId = GetTenantId();
        var result = await _mrpService.GetRunHistoryAsync(tenantId, limit);
        return Ok(result);
    }

    [HttpGet("exceptions")]
    public async Task<IActionResult> GetExceptionMessages([FromQuery] Guid? runHistoryId = null)
    {
        var tenantId = GetTenantId();
        var result = await _mrpService.GetExceptionMessagesAsync(tenantId, runHistoryId);
        return Ok(result);
    }

    [HttpPost("run-multi-plant")]
    public async Task<IActionResult> RunMrpMultiPlant([FromQuery] Guid? plantId = null)
    {
        var tenantId = GetTenantId();
        var result = await _mrpService.RunMrpMultiPlantAsync(tenantId, plantId);
        return Ok(result);
    }

    [HttpPost("capacity-leveling")]
    public async Task<IActionResult> CalculateCapacityLeveling([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var tenantId = GetTenantId();
        var result = await _mrpService.CalculateCapacityLevelingAsync(tenantId, start, end);
        return Ok(result);
    }

    [HttpPost("run-with-vendor-lt")]
    public async Task<IActionResult> RunMrpWithVendorLeadTime([FromQuery] Guid? plantId = null)
    {
        var tenantId = GetTenantId();
        var result = await _mrpService.RunMrpWithVendorLeadTimeAsync(tenantId, plantId);
        return Ok(result);
    }

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
}
