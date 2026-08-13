using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/pp")]
[Authorize]
public class ProductionController : ControllerBase
{
    private readonly IRepository<ProductionPlanEntity, Guid> _plans;
    private readonly IRepository<BillOfMaterialEntity, Guid> _bom;
    private readonly IRepository<ProductionRoutingEntity, Guid> _routing;
    private readonly IRepository<WorkCenterEntity, Guid> _workCenters;
    private readonly IRepository<ProductionOrderEntity, Guid> _orders;
    private readonly ITenantContext _tenant;

    public ProductionController(
        IRepository<ProductionPlanEntity, Guid> plans,
        IRepository<BillOfMaterialEntity, Guid> bom,
        IRepository<ProductionRoutingEntity, Guid> routing,
        IRepository<WorkCenterEntity, Guid> workCenters,
        IRepository<ProductionOrderEntity, Guid> orders,
        ITenantContext tenant)
    {
        _plans = plans;
        _bom = bom;
        _routing = routing;
        _workCenters = workCenters;
        _orders = orders;
        _tenant = tenant;
    }

    [HttpGet("plans")] public async Task<IActionResult> GetPlans() => Ok(new { data = await _plans.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("plans")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreatePlan([FromBody] ProductionPlanEntity model) { model.Id = Guid.NewGuid(); await _plans.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("bom")] public async Task<IActionResult> GetBOM() => Ok(new { data = await _bom.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("bom")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateBOM([FromBody] BillOfMaterialEntity model) { model.Id = Guid.NewGuid(); await _bom.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("routing")] public async Task<IActionResult> GetRouting() => Ok(new { data = await _routing.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("routing")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateRouting([FromBody] ProductionRoutingEntity model) { model.Id = Guid.NewGuid(); await _routing.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("workcenters")] public async Task<IActionResult> GetWorkCenters() => Ok(new { data = await _workCenters.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("workcenters")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateWorkCenter([FromBody] WorkCenterEntity model) { model.Id = Guid.NewGuid(); await _workCenters.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("production-orders")] public async Task<IActionResult> GetProductionOrders() => Ok(new { data = await _orders.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("production-orders")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateProductionOrder([FromBody] ProductionOrderEntity model) { model.Id = Guid.NewGuid(); await _orders.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
}
