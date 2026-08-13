using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/superuser")]
[Authorize(Policy = "SuperUser")]
public class SuperUserController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly IAuthService _auth;
    private readonly IAdminUserService _adminUserService;
    private readonly IAuditService _audit;
    private readonly IApprovalService _approval;
    private readonly IModuleCatalog _moduleCatalog;

    public SuperUserController(
        YuktiraDbContext db,
        IAuthService auth,
        IAdminUserService adminUserService,
        IAuditService audit,
        IApprovalService approval,
        IModuleCatalog moduleCatalog)
    {
        _db = db;
        _auth = auth;
        _adminUserService = adminUserService;
        _audit = audit;
        _approval = approval;
        _moduleCatalog = moduleCatalog;
    }

    [HttpPost("unlock-document/{documentId}")]
    public async Task<IActionResult> UnlockDocument(string documentId)
    {
        var pending = await _db.ApprovalRequests
            .Where(a => a.TenantId == _tenantId && a.Status == "Pending" &&
                        (a.Subject.Contains(documentId) || a.RequestId == documentId))
            .ToListAsync();

        var unlocked = 0;
        foreach (var req in pending)
        {
            req.Status = "Approved";
            req.UpdatedAt = DateTime.UtcNow;
            unlocked++;
        }

        var tcodeData = await _db.TCodeData
            .Where(t => t.TenantId == _tenantId && t.RecordId == documentId && t.Status != "Approved")
            .ToListAsync();
        foreach (var t in tcodeData)
        {
            t.Status = "Released";
            t.WorkflowNode = "";
            unlocked++;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = _currentUserId,
            TenantId = _tenantId,
            ModuleName = "SuperUser",
            ActionType = ActionType.Unlock,
            EntityName = "Document",
            EntityId = documentId,
            NewValue = $"Unlocked {unlocked} blocked workflow item(s)"
        });

        return Ok(new { message = $"Document {documentId} unlocked", unlockedItems = unlocked });
    }

    [HttpPost("reset-password/{userId}")]
    public async Task<IActionResult> ResetPassword(Guid userId)
    {
        var tempPassword = $"Temp@{Guid.NewGuid():N}"[..12];
        var result = await _adminUserService.ResetPasswordAsync(userId, tempPassword);
        if (!result.Success)
            return BadRequest(new { message = result.Error ?? "Password reset failed" });

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = _currentUserId,
            TenantId = _tenantId,
            ModuleName = "SuperUser",
            ActionType = ActionType.Update,
            EntityName = "AdminUser",
            EntityId = userId.ToString(),
            NewValue = "Password reset by superuser"
        });

        return Ok(new { message = $"Password reset for user {userId}", tempPassword });
    }

    [HttpPost("impersonate/{userId}")]
    public async Task<IActionResult> Impersonate(Guid userId)
    {
        try
        {
            var response = await _auth.ImpersonateAsync(_currentUserId, userId);
            return Ok(new
            {
                message = $"Impersonating user {userId}",
                accessToken = response.AccessToken,
                expiresAt = response.ExpiresAt,
                user = response.UserProfile
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("audit-logs/summary")]
    public async Task<IActionResult> GetAuditSummary()
    {
        var now = DateTime.UtcNow;
        var total = await _audit.GetLogCountAsync(_tenantId);
        var flagged = await _audit.GetFlaggedEntriesAsync(_tenantId, 1, 1000);
        var lastHour = await _audit.GetLogsAsync(_tenantId, null, null, null, now.AddHours(-1), now, 1, 1);
        var recentByModule = await _db.AuditLogs
            .Where(l => l.TenantId == _tenantId)
            .GroupBy(l => l.ModuleName)
            .Select(g => new { module = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(8)
            .ToListAsync();

        return Ok(new
        {
            totalLogs = total,
            suspiciousLogs = flagged.Count,
            lastHourLogs = lastHour.Count,
            byModule = recentByModule
        });
    }

    [HttpPost("tenants/{tenantId}/toggle-module/{moduleCode}")]
    public async Task<IActionResult> ToggleModule(Guid tenantId, string moduleCode, [FromBody] ToggleModuleRequest request)
    {
        var module = _moduleCatalog.GetModule(moduleCode);
        if (module == null)
            return BadRequest(new { message = $"Unknown module {moduleCode}" });

        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return NotFound(new { message = "Tenant not found" });

        var key = $"module.{moduleCode}.enabled";
        var setting = await _db.TenantSettings.FirstOrDefaultAsync(s =>
            s.TenantCode == tenant.Code && s.Name == key);
        if (setting == null)
        {
            _db.TenantSettings.Add(new TenantSettingEntity
            {
                Id = Guid.NewGuid(),
                TenantCode = tenant.Code,
                Name = key,
                Subdomain = tenant.Code,
                Status = request.Enable ? "Enabled" : "Disabled"
            });
        }
        else
        {
            setting.Status = request.Enable ? "Enabled" : "Disabled";
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = _currentUserId,
            TenantId = tenantId,
            ModuleName = "SuperUser",
            ActionType = ActionType.Update,
            EntityName = "TenantModule",
            EntityId = $"{tenantId}:{moduleCode}",
            NewValue = request.Enable ? "enabled" : "disabled"
        });

        return Ok(new { message = $"Module {moduleCode} {(request.Enable ? "enabled" : "disabled")} for tenant {tenantId}" });
    }

    [HttpGet("module-states/{tenantId}")]
    public async Task<IActionResult> GetModuleStates(Guid tenantId)
    {
        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant == null) return NotFound(new { message = "Tenant not found" });

        var settings = await _db.TenantSettings
            .Where(s => s.TenantCode == tenant.Code && s.Name.StartsWith("module."))
            .ToListAsync();

        var states = _moduleCatalog.Modules.ToDictionary(
            m => m.Code,
            m => settings.FirstOrDefault(s => s.Name == $"module.{m.Code}.enabled")?.Status == "Enabled");

        return Ok(new { tenantId, states });
    }

    private Guid _currentUserId =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    private Guid _tenantId =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

    public class ToggleModuleRequest { public bool Enable { get; set; } }
}