using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/wm/[controller]")]
[Authorize]
public class WarehouseController : ControllerBase
{
    private readonly ITenantContext _tenant;
    public WarehouseController(ITenantContext tenant) => _tenant = tenant;
    [HttpGet("bins")] public IActionResult GetBins() => Ok(new { data = new[] { new { Bin = "A-01-01" } }, tenantId = _tenant.TenantId });
    [HttpPost("bins")] public IActionResult CreateBin([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("transfers")] public IActionResult GetTransfers() => Ok(new { data = new[] { new { TransferId = "TRF-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("transfers")] public IActionResult CreateTransfer([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("storage-locations")] public IActionResult GetStorageLocations() => Ok(new { data = new[] { new { Code = "SL-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("storage-locations")] public IActionResult CreateStorageLocation([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
}
