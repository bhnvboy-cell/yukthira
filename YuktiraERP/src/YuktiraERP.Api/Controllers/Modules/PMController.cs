using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/pm")]
[Authorize]
public class PMController : ControllerBase
{
    private readonly IRepository<EquipmentEntity, Guid> _equipment;
    private readonly IRepository<MaintenancePlanEntity, Guid> _plans;
    private readonly IRepository<MaintenanceOrderEntity, Guid> _orders;
    private readonly ITenantContext _tenant;

    public PMController(
        IRepository<EquipmentEntity, Guid> equipment,
        IRepository<MaintenancePlanEntity, Guid> plans,
        IRepository<MaintenanceOrderEntity, Guid> orders,
        ITenantContext tenant)
    {
        _equipment = equipment;
        _plans = plans;
        _orders = orders;
        _tenant = tenant;
    }

    [HttpGet("equipment")] public async Task<IActionResult> GetEquipment() => Ok(new { data = await _equipment.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("equipment/{id:guid}")]
    public async Task<IActionResult> GetEquipmentById(Guid id)
    {
        var item = await _equipment.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("equipment")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateEquipment([FromBody] EquipmentEntity model) { model.Id = Guid.NewGuid(); await _equipment.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("equipment/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateEquipment(Guid id, [FromBody] EquipmentEntity model)
    {
        var exists = (await _equipment.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _equipment.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("equipment/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteEquipment(Guid id)
    {
        var exists = (await _equipment.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _equipment.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("maintenance-plans")] public async Task<IActionResult> GetPlans() => Ok(new { data = await _plans.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("maintenance-plans/{id:guid}")]
    public async Task<IActionResult> GetPlan(Guid id)
    {
        var item = await _plans.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("maintenance-plans")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreatePlan([FromBody] MaintenancePlanEntity model) { model.Id = Guid.NewGuid(); await _plans.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("maintenance-plans/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] MaintenancePlanEntity model)
    {
        var exists = (await _plans.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _plans.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("maintenance-plans/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        var exists = (await _plans.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _plans.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("maintenance-orders")] public async Task<IActionResult> GetOrders() => Ok(new { data = await _orders.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("maintenance-orders/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var item = await _orders.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("maintenance-orders")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateOrder([FromBody] MaintenanceOrderEntity model) { model.Id = Guid.NewGuid(); await _orders.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("maintenance-orders/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] MaintenanceOrderEntity model)
    {
        var exists = (await _orders.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _orders.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("maintenance-orders/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        var exists = (await _orders.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _orders.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
}
