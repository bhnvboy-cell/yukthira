using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/admin/users")]
[Authorize]
public class AdminUserController : ControllerBase
{
    private readonly ITenantContext _tenant;
    public AdminUserController(ITenantContext tenant) => _tenant = tenant;
    [HttpGet]
    public IActionResult GetAll() => Ok(new
    {
        data = new[]
        {
            new { UserId = "USR-001", UserName = "admin", Email = "admin@yuktira.com", Role = "SUPER_USER", IsActive = true, CreatedAt = "2025-01-01" },
            new { UserId = "USR-002", UserName = "jdoe", Email = "jdoe@yuktira.com", Role = "POWER_USER", IsActive = true, CreatedAt = "2025-02-15" },
            new { UserId = "USR-003", UserName = "asmith", Email = "asmith@yuktira.com", Role = "READ_ONLY", IsActive = false, CreatedAt = "2025-03-10" },
        },
        tenantId = _tenant.TenantId
    });

    [HttpPost] public IActionResult Create([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpDelete("{id}")] public IActionResult Delete(string id) => Ok(new { success = true, tenantId = _tenant.TenantId });
}
