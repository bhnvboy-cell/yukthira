using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/security")]
[Authorize]
public class SecurityController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IAuditService _audit;
    private readonly ISuperUserService _superUser;
    private readonly ITenantContext _tenant;
    private readonly YuktiraDbContext _db;

    public SecurityController(IAuthService auth, IAuditService audit, ISuperUserService superUser, ITenantContext tenant, YuktiraDbContext db)
    {
        _auth = auth; _audit = audit; _superUser = superUser; _tenant = tenant; _db = db;
    }

    [HttpGet("my-permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var perms = await _auth.GetUserPermissionsAsync(userId, _tenant.TenantId);
        var superPerms = await _superUser.GetPermissionsAsync(userId);
        return Ok(new { permissions = perms, superUser = superPerms, tenantId = _tenant.TenantId });
    }

    [HttpGet("permission-matrix")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> GetPermissionMatrix()
    {
        var roles = await _db.TransactionPermissions
            .Where(tp => tp.PrincipalType == "Role")
            .GroupBy(tp => tp.PrincipalValue)
            .Select(g => new { Role = g.Key, Permissions = g.Select(tp => tp.TransactionCodeId.ToString()).ToList() })
            .ToListAsync();
        var users = await _db.TransactionPermissions
            .Where(tp => tp.PrincipalType == "User")
            .GroupBy(tp => tp.PrincipalValue)
            .Select(g => new { UserId = g.Key, Permissions = g.Select(tp => tp.TransactionCodeId.ToString()).ToList() })
            .ToListAsync();
        return Ok(new { roles, users, tenantId = _tenant.TenantId });
    }

    [HttpGet("password-policy")]
    public async Task<IActionResult> GetPasswordPolicy()
    {
        var configs = await _db.SystemConfigs
            .Where(c => c.Key.StartsWith("auth."))
            .ToListAsync();
        var policy = new Dictionary<string, string>
        {
            ["min_length"] = (configs.FirstOrDefault(c => c.Key == "auth.password_min_length")?.Value) ?? "8",
            ["max_login_attempts"] = (configs.FirstOrDefault(c => c.Key == "auth.max_login_attempts")?.Value) ?? "5",
            ["lockout_minutes"] = (configs.FirstOrDefault(c => c.Key == "auth.lockout_minutes")?.Value) ?? "15",
            ["require_mfa"] = (configs.FirstOrDefault(c => c.Key == "features.enable_mfa")?.Value) ?? "false"
        };
        return Ok(new { policy, tenantId = _tenant.TenantId });
    }

    [HttpPut("password-policy")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> UpdatePasswordPolicy([FromBody] Dictionary<string, string> policy)
    {
        foreach (var kv in policy)
        {
            var key = $"auth.{kv.Key}";
            var existing = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
            if (existing != null) existing.Value = kv.Value;
            else _db.SystemConfigs.Add(new SystemConfigEntity { Key = key, Value = kv.Value, Module = "Auth" });
        }
        await _db.SaveChangesAsync();
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("suspicious-activity")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> GetSuspiciousActivity([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var entries = await _audit.GetFlaggedEntriesAsync(_tenant.TenantId, page, pageSize);
        return Ok(new { data = entries, tenantId = _tenant.TenantId });
    }

    [HttpPost("suspicious-activity/detect")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> DetectSuspiciousActivity()
    {
        var count = await _audit.DetectAndFlagSuspiciousAsync(_tenant.TenantId);
        return Ok(new { flagged = count, tenantId = _tenant.TenantId });
    }

    [HttpPost("suspicious-activity/{id}/flag")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> FlagEntry(Guid id)
    {
        await _audit.FlagSuspiciousAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("compliance/audit-log")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> GetComplianceAuditLog([FromQuery] string? module, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var logs = await _audit.GetLogsAsync(_tenant.TenantId, null, module, null, from, to, page, pageSize);
        var total = await _audit.GetLogCountAsync(_tenant.TenantId);
        return Ok(new { data = logs, total, page, pageSize, tenantId = _tenant.TenantId });
    }

    [HttpPost("compliance/export-audit")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> ExportAuditLog()
    {
        var stream = new MemoryStream();
        await _audit.ExportToCsvAsync(_tenant.TenantId, stream);
        stream.Position = 0;
        return File(stream, "text/csv", $"audit_log_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("super-user/permissions")]
    [Authorize(Policy = "SuperUser")]
    public async Task<IActionResult> GetSuperUserPermissions()
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var perms = await _superUser.GetPermissionsAsync(userId);
        return Ok(new { data = perms, tenantId = _tenant.TenantId });
    }

    [HttpPost("unlock-user/{userId}")]
    [Authorize(Policy = "SuperUser")]
    public async Task<IActionResult> UnlockUser(Guid userId)
    {
        var user = await _db.AdminUsers.FindAsync(userId);
        if (user == null) return NotFound();
        user.LockedUntil = null;
        user.FailedLoginAttempts = 0;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
}
