using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/sd/[controller]")]
[Authorize]
public class SOController : ControllerBase
{
    private readonly ITenantContext _tenant;
    public SOController(ITenantContext tenant) => _tenant = tenant;
    [HttpGet] public IActionResult GetAll() => Ok(new { data = new[] { new { OrderNumber = "SO-001" } }, tenantId = _tenant.TenantId });
    [HttpPost] public IActionResult Create([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
}
