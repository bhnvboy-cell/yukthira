using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/admin/system-config")]
[Authorize]
public class AdminSystemConfigController : ControllerBase
{
    private readonly ITenantContext _tenant;
    public AdminSystemConfigController(ITenantContext tenant) => _tenant = tenant;
    [HttpGet]
    public IActionResult GetAll() => Ok(new
    {
        data = new[]
        {
            new { Key = "app.name", Value = "Yuktira ERP Suite", Description = "Application Name", Module = "Global" },
            new { Key = "app.version", Value = "1.0.0", Description = "Application Version", Module = "Global" },
            new { Key = "auth.max_login_attempts", Value = "5", Description = "Max login attempts before lockout", Module = "Auth" },
            new { Key = "auth.password_min_length", Value = "8", Description = "Minimum password length", Module = "Auth" },
            new { Key = "email.smtp_host", Value = "smtp.yuktira.com", Description = "SMTP Server Host", Module = "Email" },
            new { Key = "email.smtp_port", Value = "587", Description = "SMTP Server Port", Module = "Email" },
            new { Key = "features.enable_mfa", Value = "false", Description = "Enable Multi-Factor Authentication", Module = "Features" },
            new { Key = "features.enable_audit", Value = "true", Description = "Enable Audit Logging", Module = "Features" },
        },
        tenantId = _tenant.TenantId
    });

    [HttpPut("{key}")] public IActionResult Update(string key, [FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
}
