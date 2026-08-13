using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/co")]
[Authorize]
public class COController : ControllerBase
{
    private readonly IRepository<CostCenterEntity, Guid> _costCenters;
    private readonly IRepository<CostElementEntity, Guid> _costElements;
    private readonly IRepository<ProfitCenterEntity, Guid> _profitCenters;
    private readonly IRepository<InternalOrderEntity, Guid> _internalOrders;
    private readonly ITenantContext _tenant;

    public COController(
        IRepository<CostCenterEntity, Guid> costCenters,
        IRepository<CostElementEntity, Guid> costElements,
        IRepository<ProfitCenterEntity, Guid> profitCenters,
        IRepository<InternalOrderEntity, Guid> internalOrders,
        ITenantContext tenant)
    {
        _costCenters = costCenters;
        _costElements = costElements;
        _profitCenters = profitCenters;
        _internalOrders = internalOrders;
        _tenant = tenant;
    }

    [HttpGet("cost-centers")] public async Task<IActionResult> GetCostCenters() => Ok(new { data = await _costCenters.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("cost-centers/{id:guid}")]
    public async Task<IActionResult> GetCostCenter(Guid id)
    {
        var item = await _costCenters.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("cost-centers")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateCostCenter([FromBody] CostCenterEntity model) { model.Id = Guid.NewGuid(); await _costCenters.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("cost-centers/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateCostCenter(Guid id, [FromBody] CostCenterEntity model)
    {
        var exists = (await _costCenters.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _costCenters.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("cost-centers/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteCostCenter(Guid id)
    {
        var exists = (await _costCenters.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _costCenters.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("cost-elements")] public async Task<IActionResult> GetCostElements() => Ok(new { data = await _costElements.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("cost-elements/{id:guid}")]
    public async Task<IActionResult> GetCostElement(Guid id)
    {
        var item = await _costElements.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("cost-elements")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateCostElement([FromBody] CostElementEntity model) { model.Id = Guid.NewGuid(); await _costElements.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("cost-elements/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateCostElement(Guid id, [FromBody] CostElementEntity model)
    {
        var exists = (await _costElements.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _costElements.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("cost-elements/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteCostElement(Guid id)
    {
        var exists = (await _costElements.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _costElements.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("profit-centers")] public async Task<IActionResult> GetProfitCenters() => Ok(new { data = await _profitCenters.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("profit-centers/{id:guid}")]
    public async Task<IActionResult> GetProfitCenter(Guid id)
    {
        var item = await _profitCenters.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("profit-centers")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateProfitCenter([FromBody] ProfitCenterEntity model) { model.Id = Guid.NewGuid(); await _profitCenters.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("profit-centers/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateProfitCenter(Guid id, [FromBody] ProfitCenterEntity model)
    {
        var exists = (await _profitCenters.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _profitCenters.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("profit-centers/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteProfitCenter(Guid id)
    {
        var exists = (await _profitCenters.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _profitCenters.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("internal-orders")] public async Task<IActionResult> GetInternalOrders() => Ok(new { data = await _internalOrders.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("internal-orders/{id:guid}")]
    public async Task<IActionResult> GetInternalOrder(Guid id)
    {
        var item = await _internalOrders.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("internal-orders")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateInternalOrder([FromBody] InternalOrderEntity model) { model.Id = Guid.NewGuid(); await _internalOrders.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("internal-orders/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateInternalOrder(Guid id, [FromBody] InternalOrderEntity model)
    {
        var exists = (await _internalOrders.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _internalOrders.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("internal-orders/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteInternalOrder(Guid id)
    {
        var exists = (await _internalOrders.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _internalOrders.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
}
