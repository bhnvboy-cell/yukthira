using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/mrp/ddmrp")]
[Authorize]
public class DdmrpController : ControllerBase
{
    private readonly IDdmrpBufferService _ddmrpService;

    public DdmrpController(IDdmrpBufferService ddmrpService)
    {
        _ddmrpService = ddmrpService;
    }

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

    [HttpGet("buffers/{materialCode}")]
    public async Task<IActionResult> GetBuffer(string materialCode, [FromQuery] string? plant = "1000")
    {
        var buffer = await _ddmrpService.CalculateBufferZonesAsync(GetTenantId(), materialCode, plant ?? "1000");
        return Ok(buffer);
    }

    [HttpGet("buffers")]
    public async Task<IActionResult> GetAllBuffers([FromQuery] string? plant = "1000")
    {
        var list = await _ddmrpService.EvaluateAllBuffersAsync(GetTenantId(), plant ?? "1000");
        return Ok(new { plant = plant ?? "1000", total = list.Count, data = list });
    }

    [HttpGet("replenishments")]
    public async Task<IActionResult> GetReplenishments([FromQuery] string? plant = "1000")
    {
        var proposals = await _ddmrpService.GetReplenishmentProposalsAsync(GetTenantId(), plant ?? "1000");
        return Ok(new { plant = plant ?? "1000", proposalCount = proposals.Count, proposals });
    }
}
