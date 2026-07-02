using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/pp/[controller]")]
[Authorize]
public class ProductionController : ControllerBase
{
    private readonly ITenantContext _tenant;
    public ProductionController(ITenantContext tenant) => _tenant = tenant;
    [HttpGet("plans")] public IActionResult GetPlans() => Ok(new { data = new[] { new { PlanId = "PP-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("plans")] public IActionResult CreatePlan([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("bom")] public IActionResult GetBOM() => Ok(new { data = new[] { new { BomId = "BOM-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("bom")] public IActionResult CreateBOM([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("routing")] public IActionResult GetRouting() => Ok(new { data = new[] { new { RoutingId = "RTG-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("routing")] public IActionResult CreateRouting([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("workcenters")] public IActionResult GetWorkCenters() => Ok(new { data = new[] { new { Code = "WC-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("workcenters")] public IActionResult CreateWorkCenter([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("production-orders")] public IActionResult GetProductionOrders() => Ok(new { data = new[] { new { OrderNumber = "MO-24001" } }, tenantId = _tenant.TenantId });
    [HttpPost("production-orders")] public IActionResult CreateProductionOrder([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
}
