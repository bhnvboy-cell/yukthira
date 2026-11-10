using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/finance/fx-valuation")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class FxValuationController : ControllerBase
{
    private readonly IFxValuationService _fxValuation;

    public FxValuationController(IFxValuationService fxValuation)
    {
        _fxValuation = fxValuation;
    }

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string? baseCurrency = "INR", [FromQuery] DateTime? valuationDate = null)
    {
        var targetDate = valuationDate ?? DateTime.UtcNow;
        var list = await _fxValuation.PreviewRevaluationAsync(GetTenantId(), baseCurrency ?? "INR", targetDate);
        return Ok(new
        {
            valuationDate = targetDate,
            baseCurrency = baseCurrency ?? "INR",
            itemCount = list.Count,
            items = list
        });
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] FxValuationRunRequest req)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var targetDate = req.ValuationDate ?? DateTime.UtcNow;
        var result = await _fxValuation.ExecuteRevaluationAsync(GetTenantId(), req.BaseCurrency ?? "INR", targetDate, req.PostEntries, userId);
        return Ok(result);
    }
}

public record FxValuationRunRequest(string? BaseCurrency = "INR", DateTime? ValuationDate = null, bool PostEntries = false);
