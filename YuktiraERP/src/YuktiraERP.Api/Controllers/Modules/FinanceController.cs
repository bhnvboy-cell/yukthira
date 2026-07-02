using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/fi/[controller]")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly ITenantContext _tenant;
    public FinanceController(ITenantContext tenant) => _tenant = tenant;
    [HttpGet("ledger")] public IActionResult GetLedger() => Ok(new { data = new[] { new { DocumentNumber = "GL-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("ledger")] public IActionResult CreateLedger([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("ap")] public IActionResult GetAP() => Ok(new { data = new[] { new { DocumentNumber = "AP-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("ap")] public IActionResult CreateAP([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("ar")] public IActionResult GetAR() => Ok(new { data = new[] { new { DocumentNumber = "AR-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("ar")] public IActionResult CreateAR([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("fixed-assets")] public IActionResult GetFixedAssets() => Ok(new { data = new[] { new { AssetCode = "FA-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("fixed-assets")] public IActionResult CreateFixedAsset([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
}
