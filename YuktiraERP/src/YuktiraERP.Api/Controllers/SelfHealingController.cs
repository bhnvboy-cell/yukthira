using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/self-healing")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class SelfHealingController : ControllerBase
{
    private readonly ISelfHealingReconciliationService _service;
    private readonly ITenantContext _tenant;
    private readonly SelfHealingOptions _options;

    public SelfHealingController(
        ISelfHealingReconciliationService service,
        ITenantContext tenant,
        IOptions<SelfHealingOptions> options)
    {
        _service = service;
        _tenant = tenant;
        _options = options?.Value ?? new SelfHealingOptions();
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run()
    {
        var result = await _service.RunOnceAsync(_tenant.TenantId, GetUserId().ToString(), HttpContext.RequestAborted);
        return Ok(new
        {
            success = true,
            tenantId = _tenant.TenantId,
            startedAt = result.StartedAt,
            finishedAt = result.FinishedAt,
            grIrScanned = result.GrIrScanned,
            pennyScanned = result.PennyScanned,
            stuckPostingsScanned = result.StuckPostingsScanned,
            appliedCount = result.AppliedCount,
            skippedCount = result.SkippedCount,
            flaggedCount = result.FlaggedCount,
            failedCount = result.FailedCount,
            actions = result.Actions
        });
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            enabled = _options.Enabled,
            intervalMinutes = _options.IntervalMinutes,
            startupDelaySeconds = _options.StartupDelaySeconds,
            pennyTolerance = _options.PennyTolerance,
            maxAutoFixAmount = _options.MaxAutoFixAmount,
            stuckPostingAgeMinutes = _options.StuckPostingAgeMinutes,
            tenantId = _tenant.TenantId
        });
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
}
