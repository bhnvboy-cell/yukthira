using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/finance/anomalies")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class FinancialAnomalyController : ControllerBase
{
    private readonly IFinancialAnomalyService _anomalyService;

    public FinancialAnomalyController(IFinancialAnomalyService anomalyService)
    {
        _anomalyService = anomalyService;
    }

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

    [HttpGet("scan-journal")]
    public async Task<IActionResult> ScanJournal([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null, [FromQuery] double thresholdZ = 2.5)
    {
        var from = fromDate ?? DateTime.UtcNow.AddMonths(-3);
        var to = toDate ?? DateTime.UtcNow;
        var report = await _anomalyService.ScanJournalAnomaliesAsync(GetTenantId(), from, to, thresholdZ);
        return Ok(report);
    }

    [HttpGet("scan-procurement")]
    public async Task<IActionResult> ScanProcurement([FromQuery] double thresholdZ = 2.5)
    {
        var report = await _anomalyService.ScanProcurementAnomaliesAsync(GetTenantId(), thresholdZ);
        return Ok(report);
    }
}
