using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/ai/nl2sql")]
[Authorize]
public class Nl2SqlController : ControllerBase
{
    private readonly INl2SqlService _service;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public Nl2SqlController(INl2SqlService service, ITenantContext tenant, IAuditService audit)
    {
        _service = service;
        _tenant = tenant;
        _audit = audit;
    }

    [HttpPost("query")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Query([FromBody] Nl2SqlQueryRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "text is required. Example: show 10 purchase orders" });
        }

        request.TenantId = _tenant.TenantId;
        request.UserId = GetUserId().ToString();

        var result = await _service.QueryAsync(request);

        await _audit.LogAsync(new AuditEntryDto
        {
            Timestamp = DateTime.UtcNow,
            UserId = GetUserId(),
            TenantId = _tenant.TenantId,
            ModuleName = "Analytics",
            ActionType = result.Success ? ActionType.ApiCall : ActionType.Config,
            EntityName = "Nl2SqlQuery",
            EntityId = result.Entity,
            Details = $"{(result.Success ? "OK" : "Rejected")}: {request.Text}; intent={result.Intent}; entity={result.Entity}; rows={result.RowCount}; error={result.Error}"
        });

        if (!result.Success)
        {
            return BadRequest(new
            {
                error = result.Error,
                intent = result.Intent,
                entity = result.Entity
            });
        }

        return Ok(new
        {
            intent = result.Intent,
            entity = result.Entity,
            sql = result.Sql,
            columns = result.Columns,
            rows = result.Rows,
            rowCount = result.RowCount,
            executedMs = result.ExecutedMs
        });
    }

    [HttpPost("draft")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Draft([FromBody] Nl2SqlDraftRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "text is required. Example: draft non-conformance ticket if defect rate > 5" });
        }

        request.TenantId = _tenant.TenantId;
        request.UserId = GetUserId().ToString();

        var result = await _service.DraftAsync(request);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new
        {
            condition = result.Condition,
            value = result.Value,
            threshold = result.Threshold,
            drafted = result.Drafted,
            nonConformanceId = result.NonConformanceId
        });
    }

    [HttpGet("intents")]
    public IActionResult Intents()
    {
        return Ok(new { intents = _service.GetSupportedIntents() });
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
}
