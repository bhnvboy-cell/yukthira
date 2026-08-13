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
    [HttpPost("equipment")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateEquipment([FromBody] EquipmentEntity model) { model.Id = Guid.NewGuid(); await _equipment.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("maintenance-plans")] public async Task<IActionResult> GetPlans() => Ok(new { data = await _plans.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("maintenance-plans")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreatePlan([FromBody] MaintenancePlanEntity model) { model.Id = Guid.NewGuid(); await _plans.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("maintenance-orders")] public async Task<IActionResult> GetOrders() => Ok(new { data = await _orders.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("maintenance-orders")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateOrder([FromBody] MaintenanceOrderEntity model) { model.Id = Guid.NewGuid(); await _orders.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
}
