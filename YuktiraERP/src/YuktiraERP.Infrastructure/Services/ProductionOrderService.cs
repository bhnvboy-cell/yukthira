using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IProductionOrderService
{
    Task<ProductionOrderEntity> ReleaseOrderAsync(Guid orderId, string userId);
    Task<ProductionOrderEntity> StartProductionAsync(Guid orderId, string userId);
    Task<ProductionOrderEntity> ConfirmProductionAsync(Guid orderId, decimal yieldQty, decimal scrapQty, string userId);
    Task<ProductionOrderEntity> CompleteOrderAsync(Guid orderId, string userId);
    Task<ProductionOrderEntity> TecoOrderAsync(Guid orderId, string userId);
    Task<ProductionOrderEntity> CancelOrderAsync(Guid orderId, string reason, string userId);
    Task<List<GoodsMovementResult>> PostGoodsIssueAsync(Guid orderId, List<ComponentIssue> components, string userId);
    Task<GoodsMovementResult> PostGoodsReceiptAsync(Guid orderId, decimal quantity, string batchNo, string userId);
    Task<ProductionCostSummary> GetOrderCostsAsync(Guid orderId);
    Task<MaterialStagingResult> StageMaterialsAsync(Guid orderId, List<StagingRequest> materials, string userId);
}

public class ComponentIssue
{
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? BatchNo { get; set; }
}

public class GoodsMovementResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Guid MovementId { get; set; }
    public decimal QuantityPosted { get; set; }
}

public class ProductionCostSummary
{
    public decimal PlannedMaterialCost { get; set; }
    public decimal ActualMaterialCost { get; set; }
    public decimal PlannedLaborCost { get; set; }
    public decimal ActualLaborCost { get; set; }
    public decimal PlannedOverheadCost { get; set; }
    public decimal ActualOverheadCost { get; set; }
    public decimal TotalPlannedCost { get; set; }
    public decimal TotalActualCost { get; set; }
    public decimal Variance => TotalActualCost - TotalPlannedCost;
    public List<ComponentCostDetail> ComponentCosts { get; set; } = new();
}

public class ComponentCostDetail
{
    public string MaterialName { get; set; } = "";
    public decimal PlannedQty { get; set; }
    public decimal ActualQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal PlannedCost => PlannedQty * UnitPrice;
    public decimal ActualCost => ActualQty * UnitPrice;
}

public class StagingRequest
{
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string? BatchNo { get; set; }
}

public class MaterialStagingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int MaterialsStaged { get; set; }
    public int MaterialsFailed { get; set; }
    public List<MaterialStagingEntity> StagingRecords { get; set; } = new();
}

public class ProductionOrderService : IProductionOrderService
{
    private readonly YuktiraDbContext _db;
    private readonly IInventoryService _inventoryService;
    private readonly IGoodsMovementService _goodsMovementService;

    public ProductionOrderService(
        YuktiraDbContext db,
        IInventoryService inventoryService,
        IGoodsMovementService goodsMovementService)
    {
        _db = db;
        _inventoryService = inventoryService;
        _goodsMovementService = goodsMovementService;
    }

    public async Task<ProductionOrderEntity> ReleaseOrderAsync(Guid orderId, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        if (!order.CanTransitionTo("RELEASED"))
            throw new InvalidOperationException($"Cannot release order in {order.Status} status");

        if (order.BOMId.HasValue)
        {
            var bomExists = await _db.BillOfMaterials.AnyAsync(b => b.Id == order.BOMId.Value && b.Status == "Active");
            if (!bomExists)
                throw new InvalidOperationException("Referenced BOM does not exist or is not active");
        }

        var components = await _db.BillOfMaterials
            .Where(b => b.ProductName == order.ProductName && b.Status == "Active")
            .ToListAsync();

        foreach (var comp in components)
        {
            var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == comp.ComponentName);
            if (material == null)
                throw new InvalidOperationException($"Material {comp.ComponentName} not found in material master");

            var requiredQty = order.Quantity * comp.Quantity;
            var atp = await _inventoryService.CheckAvailabilityAsync(material.Id, requiredQty, order.StartDate);
            if (!atp.IsAvailable)
                throw new InvalidOperationException($"Insufficient stock for {comp.ComponentName}. Available: {atp.AvailableQuantity}, Required: {requiredQty}");
        }

        order.TransitionTo("RELEASED");
        order.ReleasedAt = DateTime.UtcNow;
        order.ReleaseBy = userId;
        await _db.SaveChangesAsync();

        return order;
    }

    public async Task<ProductionOrderEntity> StartProductionAsync(Guid orderId, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        if (!order.CanTransitionTo("IN_PROGRESS"))
            throw new InvalidOperationException($"Cannot start production for order in {order.Status} status");

        order.TransitionTo("IN_PROGRESS");
        await _db.SaveChangesAsync();

        return order;
    }

    public async Task<ProductionOrderEntity> ConfirmProductionAsync(Guid orderId, decimal yieldQty, decimal scrapQty, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        if (!order.CanTransitionTo("COMPLETED"))
            throw new InvalidOperationException($"Cannot confirm production for order in {order.Status} status");

        var receiptResult = await PostGoodsReceiptAsync(orderId, yieldQty, order.BatchNo ?? $"BATCH-{order.OrderNumber}", userId);

        var components = await _db.BillOfMaterials
            .Where(b => b.ProductName == order.ProductName && b.Status == "Active")
            .ToListAsync();

        foreach (var comp in components)
        {
            var componentQty = order.Quantity * comp.Quantity;
            await PostGoodsIssueAsync(orderId, new List<ComponentIssue>
            {
                new() { MaterialName = comp.ComponentName, Quantity = componentQty }
            }, userId);
        }

        order.YieldQty = yieldQty;
        order.ScrapQty = scrapQty;
        order.TransitionTo("COMPLETED");
        order.ConfirmedAt = DateTime.UtcNow;
        order.ConfirmBy = userId;
        await _db.SaveChangesAsync();

        return order;
    }

    public async Task<ProductionOrderEntity> CompleteOrderAsync(Guid orderId, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        if (order.Status != "COMPLETED")
            throw new InvalidOperationException("Order must be in COMPLETED status before final completion");

        return order;
    }

    public async Task<ProductionOrderEntity> TecoOrderAsync(Guid orderId, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        if (!order.CanTransitionTo("TECO"))
            throw new InvalidOperationException($"Cannot TECO order in {order.Status} status");

        var hasOpenReservations = await _db.Set<StockReservationEntity>()
            .AnyAsync(r => r.OrderId == orderId && r.Status == "Active");
        if (hasOpenReservations)
            throw new InvalidOperationException("Cannot TECO: order has open stock reservations");

        order.TransitionTo("TECO");
        order.TecodAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return order;
    }

    public async Task<ProductionOrderEntity> CancelOrderAsync(Guid orderId, string reason, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        if (!order.CanTransitionTo("CANCELLED"))
            throw new InvalidOperationException($"Cannot cancel order in {order.Status} status");

        var movements = await _db.StockMovements
            .Where(m => m.Reference == order.OrderNumber && m.Status == "Posted")
            .ToListAsync();

        foreach (var movement in movements)
        {
            var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == movement.MaterialName);
            if (material != null)
            {
                if (movement.MovementType == "GI")
                    material.Stock += movement.Quantity;
                else if (movement.MovementType == "GR")
                    material.Stock -= movement.Quantity;

                material.UpdatedAt = DateTime.UtcNow;
            }
            movement.Status = "Reversed";
        }

        var reservations = await _db.Set<StockReservationEntity>()
            .Where(r => r.OrderId == orderId && r.Status == "Active")
            .ToListAsync();
        foreach (var res in reservations)
        {
            res.Status = "Released";
            res.ReleasedAt = DateTime.UtcNow;
        }

        order.TransitionTo("CANCELLED");
        order.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return order;
    }

    public async Task<List<GoodsMovementResult>> PostGoodsIssueAsync(Guid orderId, List<ComponentIssue> components, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        var results = new List<GoodsMovementResult>();

        foreach (var comp in components)
        {
            var result = await _goodsMovementService.PostGoodsIssueAsync(
                comp.MaterialName, comp.Quantity,
                "PRODUCTION_ISSUE", order.OrderNumber, "GI", userId);
            results.Add(result);

            var orderItem = await _db.ProductionOrderItems
                .FirstOrDefaultAsync(i => i.ProductionOrderId == orderId && i.MaterialName == comp.MaterialName);
            if (orderItem != null)
            {
                orderItem.IssuedQty += comp.Quantity;
            }
            else
            {
                _db.ProductionOrderItems.Add(new ProductionOrderItemEntity
                {
                    ProductionOrderId = orderId,
                    MaterialName = comp.MaterialName,
                    RequiredQty = comp.Quantity,
                    IssuedQty = comp.Quantity,
                    Status = "ISSUED"
                });
            }
        }

        await _db.SaveChangesAsync();
        return results;
    }

    public async Task<GoodsMovementResult> PostGoodsReceiptAsync(Guid orderId, decimal quantity, string batchNo, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        return await _goodsMovementService.PostGoodsReceiptAsync(
            order.ProductName, quantity, batchNo, "FG", order.OrderNumber, userId);
    }

    public async Task<ProductionCostSummary> GetOrderCostsAsync(Guid orderId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        var components = await _db.BillOfMaterials
            .Where(b => b.ProductName == order.ProductName && b.Status == "Active")
            .ToListAsync();

        var summary = new ProductionCostSummary();
        decimal totalPlannedMaterial = 0;
        decimal totalActualMaterial = 0;

        foreach (var comp in components)
        {
            var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == comp.ComponentName);
            var unitPrice = material?.Price ?? 0;
            var plannedQty = order.Quantity * comp.Quantity;

            var orderItem = await _db.ProductionOrderItems
                .FirstOrDefaultAsync(i => i.ProductionOrderId == orderId && i.MaterialName == comp.ComponentName);
            var actualQty = orderItem?.IssuedQty ?? 0;

            summary.ComponentCosts.Add(new ComponentCostDetail
            {
                MaterialName = comp.ComponentName,
                PlannedQty = plannedQty,
                ActualQty = actualQty,
                UnitPrice = unitPrice
            });

            totalPlannedMaterial += plannedQty * unitPrice;
            totalActualMaterial += actualQty * unitPrice;
        }

        summary.PlannedMaterialCost = totalPlannedMaterial;
        summary.ActualMaterialCost = totalActualMaterial;
        summary.PlannedLaborCost = order.PlannedCost * 0.3m;
        summary.ActualLaborCost = order.ActualCost * 0.3m;
        summary.PlannedOverheadCost = order.PlannedCost * 0.2m;
        summary.ActualOverheadCost = order.ActualCost * 0.2m;
        summary.TotalPlannedCost = order.PlannedCost > 0 ? order.PlannedCost : totalPlannedMaterial * 1.5m;
        summary.TotalActualCost = order.ActualCost > 0 ? order.ActualCost : totalActualMaterial + summary.ActualLaborCost + summary.ActualOverheadCost;

        return summary;
    }

    public async Task<MaterialStagingResult> StageMaterialsAsync(Guid orderId, List<StagingRequest> materials, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(orderId)
            ?? throw new InvalidOperationException("Production order not found");

        if (order.Status != "PLANNED" && order.Status != "RELEASED")
            throw new InvalidOperationException("Materials can only be staged for PLANNED or RELEASED orders");

        var result = new MaterialStagingResult();

        foreach (var mat in materials)
        {
            var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == mat.MaterialName);
            if (material == null)
            {
                result.MaterialsFailed++;
                continue;
            }

            var available = await _inventoryService.GetAvailableQuantityAsync(material.Id);
            if (available < mat.Quantity)
            {
                result.MaterialsFailed++;
                continue;
            }

            var reservation = await _inventoryService.ReserveStockAsync(material.Id, mat.Quantity, orderId);

            var staging = new MaterialStagingEntity
            {
                ProductionOrderId = orderId,
                MaterialName = mat.MaterialName,
                RequiredQty = mat.Quantity,
                StagedQty = mat.Quantity,
                Status = "STAGED"
            };
            _db.MaterialStagings.Add(staging);
            result.StagingRecords.Add(staging);
            result.MaterialsStaged++;
        }

        await _db.SaveChangesAsync();
        result.Success = result.MaterialsFailed == 0;
        result.Message = result.Success
            ? $"Staged {result.MaterialsStaged} materials successfully"
            : $"Staged {result.MaterialsStaged}, failed {result.MaterialsFailed}";

        return result;
    }
}
