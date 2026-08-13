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
    [HttpPost("cost-centers")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateCostCenter([FromBody] CostCenterEntity model) { model.Id = Guid.NewGuid(); await _costCenters.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("cost-elements")] public async Task<IActionResult> GetCostElements() => Ok(new { data = await _costElements.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("cost-elements")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateCostElement([FromBody] CostElementEntity model) { model.Id = Guid.NewGuid(); await _costElements.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("profit-centers")] public async Task<IActionResult> GetProfitCenters() => Ok(new { data = await _profitCenters.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("profit-centers")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateProfitCenter([FromBody] ProfitCenterEntity model) { model.Id = Guid.NewGuid(); await _profitCenters.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("internal-orders")] public async Task<IActionResult> GetInternalOrders() => Ok(new { data = await _internalOrders.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("internal-orders")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateInternalOrder([FromBody] InternalOrderEntity model) { model.Id = Guid.NewGuid(); await _internalOrders.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
}
