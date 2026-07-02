using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/superuser")]
[Authorize(Policy = "SuperUser")]
public class SuperUserController : ControllerBase
{
    [HttpPost("unlock-document/{documentId}")]
    public IActionResult UnlockDocument(string documentId)
        => Ok(new { message = $"Document {documentId} unlocked" });

    [HttpPost("reset-password/{userId}")]
    public IActionResult ResetPassword(Guid userId)
        => Ok(new { message = $"Password reset initiated for user {userId}", tempPassword = "Temp@123" });

    [HttpPost("impersonate/{userId}")]
    public IActionResult Impersonate(Guid userId)
        => Ok(new { message = $"Impersonating user {userId}" });

    [HttpGet("audit-logs/summary")]
    public IActionResult GetAuditSummary()
        => Ok(new { totalLogs = 1234, suspiciousLogs = 5, lastHourLogs = 23 });

    [HttpPost("tenants/{tenantId}/toggle-module/{moduleCode}")]
    public IActionResult ToggleModule(Guid tenantId, string moduleCode, [FromBody] ToggleModuleRequest request)
        => Ok(new { message = $"Module {moduleCode} {(request.Enable ? "enabled" : "disabled")} for tenant {tenantId}" });

    public class ToggleModuleRequest { public bool Enable { get; set; } }
}
