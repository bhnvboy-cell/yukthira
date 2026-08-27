using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

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
    private readonly IProductionOrderService _productionOrderService;
    private readonly IGoodsMovementService _goodsMovementService;

    public ProductionController(
        IRepository<ProductionPlanEntity, Guid> plans,
        IRepository<BillOfMaterialEntity, Guid> bom,
        IRepository<ProductionRoutingEntity, Guid> routing,
        IRepository<WorkCenterEntity, Guid> workCenters,
        IRepository<ProductionOrderEntity, Guid> orders,
        ITenantContext tenant,
        IProductionOrderService productionOrderService,
        IGoodsMovementService goodsMovementService)
    {
        _plans = plans;
        _bom = bom;
        _routing = routing;
        _workCenters = workCenters;
        _orders = orders;
        _tenant = tenant;
        _productionOrderService = productionOrderService;
        _goodsMovementService = goodsMovementService;
    }

    [HttpGet("plans")] public async Task<IActionResult> GetPlans() => Ok(new { data = await _plans.FindAsync(p => p.TenantId == _tenant.TenantId), tenantId = _tenant.TenantId });
    [HttpPost("plans")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreatePlan([FromBody] ProductionPlanEntity model) { model.Id = Guid.NewGuid(); model.TenantId = _tenant.TenantId; await _plans.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("bom")] public async Task<IActionResult> GetBOM() => Ok(new { data = await _bom.FindAsync(b => b.TenantId == _tenant.TenantId), tenantId = _tenant.TenantId });
    [HttpPost("bom")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateBOM([FromBody] BillOfMaterialEntity model) { model.Id = Guid.NewGuid(); model.TenantId = _tenant.TenantId; await _bom.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("routing")] public async Task<IActionResult> GetRouting() => Ok(new { data = await _routing.FindAsync(r => r.TenantId == _tenant.TenantId), tenantId = _tenant.TenantId });
    [HttpPost("routing")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateRouting([FromBody] ProductionRoutingEntity model) { model.Id = Guid.NewGuid(); model.TenantId = _tenant.TenantId; await _routing.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("workcenters")] public async Task<IActionResult> GetWorkCenters() => Ok(new { data = await _workCenters.FindAsync(w => w.TenantId == _tenant.TenantId), tenantId = _tenant.TenantId });
    [HttpPost("workcenters")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateWorkCenter([FromBody] WorkCenterEntity model) { model.Id = Guid.NewGuid(); model.TenantId = _tenant.TenantId; await _workCenters.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("production-orders")] public async Task<IActionResult> GetProductionOrders() => Ok(new { data = await _orders.FindAsync(o => o.TenantId == _tenant.TenantId), tenantId = _tenant.TenantId });
    [HttpPost("production-orders")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateProductionOrder([FromBody] ProductionOrderEntity model) { model.Id = Guid.NewGuid(); model.TenantId = _tenant.TenantId; await _orders.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpPost("order/{id}/release")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> ReleaseOrder(Guid id)
    {
        try
        {
            var order = await _productionOrderService.ReleaseOrderAsync(id, User.Identity?.Name ?? "system");
            return Ok(new { success = true, order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("order/{id}/start")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> StartProduction(Guid id)
    {
        try
        {
            var order = await _productionOrderService.StartProductionAsync(id, User.Identity?.Name ?? "system");
            return Ok(new { success = true, order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("order/{id}/confirm")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> ConfirmProduction(Guid id, [FromBody] ConfirmProductionRequest request)
    {
        try
        {
            var order = await _productionOrderService.ConfirmProductionAsync(id, request.YieldQty, request.ScrapQty, User.Identity?.Name ?? "system");
            return Ok(new { success = true, order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("order/{id}/complete")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CompleteOrder(Guid id)
    {
        try
        {
            var order = await _productionOrderService.CompleteOrderAsync(id, User.Identity?.Name ?? "system");
            return Ok(new { success = true, order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("order/{id}/teco")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> TecoOrder(Guid id)
    {
        try
        {
            var order = await _productionOrderService.TecoOrderAsync(id, User.Identity?.Name ?? "system");
            return Ok(new { success = true, order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("order/{id}/cancel")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var order = await _productionOrderService.CancelOrderAsync(id, request.Reason, User.Identity?.Name ?? "system");
            return Ok(new { success = true, order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("order/{id}/goods-issue")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PostGoodsIssue(Guid id, [FromBody] List<ComponentIssue> components)
    {
        try
        {
            var results = await _productionOrderService.PostGoodsIssueAsync(id, components, User.Identity?.Name ?? "system");
            return Ok(new { success = true, results });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("order/{id}/goods-receipt")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PostGoodsReceipt(Guid id, [FromBody] GoodsReceiptRequest request)
    {
        try
        {
            var result = await _productionOrderService.PostGoodsReceiptAsync(id, request.Quantity, request.BatchNo, User.Identity?.Name ?? "system");
            return Ok(new { success = true, result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("order/{id}/costs")]
    public async Task<IActionResult> GetOrderCosts(Guid id)
    {
        try
        {
            var costs = await _productionOrderService.GetOrderCostsAsync(id);
            return Ok(new { data = costs });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("order/{id}/material-staging")]
    public async Task<IActionResult> GetMaterialStaging(Guid id)
    {
        var staging = await _db.MaterialStagings
            .Where(s => s.ProductionOrderId == id)
            .ToListAsync();
        return Ok(new { data = staging });
    }

    [HttpPost("order/{id}/stage-materials")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> StageMaterials(Guid id, [FromBody] List<StagingRequest> materials)
    {
        try
        {
            var result = await _productionOrderService.StageMaterialsAsync(id, materials, User.Identity?.Name ?? "system");
            return Ok(new { success = true, result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("goods-issue")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PostGoodsIssueDirect([FromBody] DirectGoodsIssueRequest request)
    {
        try
        {
            var result = await _goodsMovementService.PostGoodsIssueAsync(
                request.MaterialName, request.Quantity, request.Reason,
                request.Reference, request.MovementType, User.Identity?.Name ?? "system");
            return Ok(new { success = true, result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("goods-receipt")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PostGoodsReceiptDirect([FromBody] DirectGoodsReceiptRequest request)
    {
        try
        {
            var result = await _goodsMovementService.PostGoodsReceiptAsync(
                request.MaterialName, request.Quantity, request.BatchNo,
                request.StorageLocation, request.Reference, User.Identity?.Name ?? "system");
            return Ok(new { success = true, result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("transfer")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PostTransfer([FromBody] TransferRequest request)
    {
        try
        {
            var result = await _goodsMovementService.PostTransferAsync(
                request.MaterialName, request.Quantity,
                request.FromLocation, request.ToLocation, User.Identity?.Name ?? "system");
            return Ok(new { success = true, result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("material-history/{materialName}")]
    public async Task<IActionResult> GetMaterialHistory(string materialName, [FromQuery] int days = 30)
    {
        var history = await _goodsMovementService.GetMaterialHistoryAsync(materialName, days);
        return Ok(new { data = history });
    }

    private YuktiraDbContext _db => HttpContext.RequestServices.GetRequiredService<YuktiraDbContext>();
}

public class ConfirmProductionRequest
{
    public decimal YieldQty { get; set; }
    public decimal ScrapQty { get; set; }
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = "";
}

public class GoodsReceiptRequest
{
    public decimal Quantity { get; set; }
    public string BatchNo { get; set; } = "";
}

public class DirectGoodsIssueRequest
{
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = "";
    public string Reference { get; set; } = "";
    public string MovementType { get; set; } = "GI";
}

public class DirectGoodsReceiptRequest
{
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string BatchNo { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string Reference { get; set; } = "";
}

public class TransferRequest
{
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string FromLocation { get; set; } = "";
    public string ToLocation { get; set; } = "";
}
