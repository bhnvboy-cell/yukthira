using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOrAbove")]
public class NumberRangesController : ControllerBase
{
    private readonly INumberRangeService _numberRangeService;

    public NumberRangesController(INumberRangeService numberRangeService) => _numberRangeService = numberRangeService;

    [HttpGet("next/{module}/{prefix}")]
    public async Task<IActionResult> GetNextNumber(string module, string prefix)
    {
        var tenantId = GetTenantId();
        var next = await _numberRangeService.GetNextNumberAsync(tenantId, module, prefix);
        return Ok(new { number = next });
    }

    [HttpGet("current/{module}/{prefix}")]
    public async Task<IActionResult> GetCurrentNumber(string module, string prefix)
    {
        var tenantId = GetTenantId();
        var current = await _numberRangeService.GetCurrentNumberAsync(tenantId, module, prefix);
        return Ok(new { current });
    }

    [HttpPost("reset/{module}/{prefix}")]
    public async Task<IActionResult> Reset(string module, string prefix, [FromBody] ResetNumberRequest request)
    {
        var tenantId = GetTenantId();
        await _numberRangeService.ResetNumberAsync(tenantId, module, prefix, request.NextNumber);
        return Ok(new { message = "Number range reset" });
    }

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
}

public class ResetNumberRequest { public long NextNumber { get; set; } }
