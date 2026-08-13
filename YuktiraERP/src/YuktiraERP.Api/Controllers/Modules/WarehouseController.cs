using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/wm")]
[Authorize]
public class WarehouseController : ControllerBase
{
    private readonly IRepository<WarehouseTransferEntity, Guid> _transfers;
    private readonly IRepository<StorageLocationEntity, Guid> _locations;
    private readonly ITenantContext _tenant;

    public WarehouseController(
        IRepository<WarehouseTransferEntity, Guid> transfers,
        IRepository<StorageLocationEntity, Guid> locations,
        ITenantContext tenant)
    {
        _transfers = transfers;
        _locations = locations;
        _tenant = tenant;
    }

    [HttpGet("bins")]
    public async Task<IActionResult> GetBins()
    {
        var locations = await _locations.GetAllAsync();
        return Ok(new { data = locations, tenantId = _tenant.TenantId });
    }

    [HttpGet("transfers")] public async Task<IActionResult> GetTransfers() => Ok(new { data = await _transfers.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("transfers")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateTransfer([FromBody] WarehouseTransferEntity model) { model.Id = Guid.NewGuid(); await _transfers.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("storage-locations")] public async Task<IActionResult> GetStorageLocations() => Ok(new { data = await _locations.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("storage-locations")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateStorageLocation([FromBody] StorageLocationEntity model) { model.Id = Guid.NewGuid(); await _locations.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
}
