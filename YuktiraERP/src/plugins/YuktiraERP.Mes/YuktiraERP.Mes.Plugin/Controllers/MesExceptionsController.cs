using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/exceptions")]
public class MesExceptionsController : MesControllerBase
{
    private readonly MesErpPostingBridge _bridge;

    public MesExceptionsController(MesErpPostingBridge bridge) => _bridge = bridge;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await _bridge.GetExceptionsAsync(CurrentTenantId, ct));

    [HttpPost("{postingLogId:guid}/retry")]
    public async Task<IActionResult> Retry(Guid postingLogId, CancellationToken ct)
        => Ok(await _bridge.RetryPostingAsync(CurrentTenantId, postingLogId, ct));
}
