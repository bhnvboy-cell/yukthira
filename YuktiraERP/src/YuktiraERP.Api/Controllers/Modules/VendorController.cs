using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/mm/[controller]")]
[Authorize]
public class VendorController : ControllerBase
{
    private readonly ITenantContext _tenant;
    public VendorController(ITenantContext tenant) => _tenant = tenant;
    [HttpGet] public IActionResult GetAll() => Ok(new { data = new[] { new { Code = "VEN-001", Name = "Sample Vendor" } }, tenantId = _tenant.TenantId });
    [HttpPost] public IActionResult Create([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
}
