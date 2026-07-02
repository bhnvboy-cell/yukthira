using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOrAbove")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService) => _auditService = auditService;

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] Guid? tenantId, [FromQuery] Guid? userId, [FromQuery] string? module, [FromQuery] ActionType? actionType, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var logs = await _auditService.GetLogsAsync(tenantId, userId, module, actionType, from, to, page, pageSize);
        return Ok(logs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLogById(Guid id)
    {
        var log = await _auditService.GetLogByIdAsync(id);
        if (log == null) return NotFound();
        return Ok(log);
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount([FromQuery] Guid? tenantId)
    {
        var count = await _auditService.GetLogCountAsync(tenantId);
        return Ok(new { count });
    }

    [HttpPost("{id}/flag-suspicious")]
    [Authorize(Policy = "SuperUser")]
    public async Task<IActionResult> FlagSuspicious(Guid id)
    {
        await _auditService.FlagSuspiciousAsync(id);
        return Ok(new { message = "Flagged as suspicious" });
    }
}
