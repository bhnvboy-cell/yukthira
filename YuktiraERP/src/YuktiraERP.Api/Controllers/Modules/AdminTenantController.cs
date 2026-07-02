using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/admin/tenants")]
[Authorize]
public class AdminTenantController : ControllerBase
{
    private readonly ITenantContext _tenant;
    public AdminTenantController(ITenantContext tenant) => _tenant = tenant;
    [HttpGet]
    public IActionResult GetAll() => Ok(new
    {
        data = new[]
        {
            new { TenantId = "TNT-001", Name = "Demo Company", Subdomain = "demo", Status = "Active", CreatedAt = "2025-01-01" },
            new { TenantId = "TNT-002", Name = "Test Corp", Subdomain = "test", Status = "Active", CreatedAt = "2025-02-01" },
            new { TenantId = "TNT-003", Name = "Dev Instance", Subdomain = "dev", Status = "Inactive", CreatedAt = "2025-03-01" },
        },
        tenantId = _tenant.TenantId
    });

    [HttpPost] public IActionResult Create([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpDelete("{id}")] public IActionResult Delete(string id) => Ok(new { success = true, tenantId = _tenant.TenantId });
}
