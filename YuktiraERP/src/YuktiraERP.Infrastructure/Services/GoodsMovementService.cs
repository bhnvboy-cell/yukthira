using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IGoodsMovementService
{
    Task<GoodsMovementResult> PostGoodsIssueAsync(string materialName, decimal quantity, string reason, string reference, string movementType, string userId);
    Task<GoodsMovementResult> PostGoodsReceiptAsync(string materialName, decimal quantity, string batchNo, string storageLocation, string reference, string userId);
    Task<GoodsMovementResult> PostTransferAsync(string materialName, decimal quantity, string fromLocation, string toLocation, string userId);
    Task<List<StockMovementEntity>> GetMaterialHistoryAsync(string materialName, int days = 30);
    Task<BackflushResult> ExecuteBackflushAsync(Guid productionOrderId, string userId);
}

public class BackflushResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int ComponentsPosted { get; set; }
    public List<GoodsMovementResult> Movements { get; set; } = new();
}

public class GoodsMovementService : IGoodsMovementService
{
    private readonly YuktiraDbContext _db;
    private readonly YuktiraERP.Core.Interfaces.IMovementTypeEngineService? _mvtEngine;

    public GoodsMovementService(YuktiraDbContext db)
    {
        _db = db;
        _mvtEngine = null;
    }

    public GoodsMovementService(YuktiraDbContext db, YuktiraERP.Core.Interfaces.IMovementTypeEngineService mvtEngine)
    {
        _db = db;
        _mvtEngine = mvtEngine;
    }

    public async Task<GoodsMovementResult> PostGoodsIssueAsync(string materialName, decimal quantity, string reason, string reference, string movementType, string userId)
    {
        var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == materialName)
            ?? throw new InvalidOperationException($"Material {materialName} not found");

        if (_mvtEngine != null && int.TryParse(movementType, out var mvtNum))
        {
            var tenantId = material.Id != Guid.Empty ? Guid.Empty : Guid.Empty;
            var mvtConfig = await _mvtEngine.GetMovementTypeAsync(mvtNum, tenantId);
            if (mvtConfig != null)
            {
                if (!mvtConfig.IsActive)
                    throw new InvalidOperationException($"Movement type {mvtNum} is inactive");

                if (!mvtConfig.AllowsNegativeStock && material.Stock < quantity)
                    throw new InvalidOperationException($"Insufficient stock for {materialName}. Available: {material.Stock}, Requested: {quantity}");

                if (mvtConfig.RequiresReference && string.IsNullOrEmpty(reference))
                    throw new InvalidOperationException($"Movement type {mvtNum} requires a reference document");
            }
        }
        else
        {
            if (material.Stock < quantity)
                throw new InvalidOperationException($"Insufficient stock for {materialName}. Available: {material.Stock}, Requested: {quantity}");
        }

        var stockBefore = material.Stock;
        material.Stock -= quantity;
        material.UpdatedAt = DateTime.UtcNow;

        var movement = new StockMovementEntity
        {
            DocumentNumber = $"GI-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6]}",
            MaterialName = materialName,
            MovementType = movementType,
            Quantity = quantity,
            StockBefore = stockBefore,
            StockAfter = material.Stock,
            Reference = reference,
            Status = "Posted"
        };
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();

        return new GoodsMovementResult
        {
            Success = true,
            Message = $"Goods issue posted for {quantity} {materialName}",
            MovementId = movement.Id,
            QuantityPosted = quantity
        };
    }

    public async Task<GoodsMovementResult> PostGoodsReceiptAsync(string materialName, decimal quantity, string batchNo, string storageLocation, string reference, string userId)
    {
        var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == materialName)
            ?? throw new InvalidOperationException($"Material {materialName} not found");

        var stockBefore = material.Stock;
        material.Stock += quantity;
        material.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(batchNo))
        {
            var existingBatch = await _db.Batches.FirstOrDefaultAsync(b => b.BatchNumber == batchNo && b.MaterialName == materialName);
            if (existingBatch != null)
            {
                existingBatch.Quantity += quantity;
                existingBatch.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Batches.Add(new BatchEntity
                {
                    BatchNumber = batchNo,
                    MaterialId = material.Id,
                    MaterialName = materialName,
                    ManufacturingDate = DateTime.UtcNow,
                    Status = "ACTIVE",
                    Quantity = quantity,
                    UnitOfMeasure = material.UOM,
                    StorageLocationName = storageLocation
                });
            }
        }

        var movement = new StockMovementEntity
        {
            DocumentNumber = $"GR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6]}",
            MaterialName = materialName,
            MovementType = "GR",
            Quantity = quantity,
            StockBefore = stockBefore,
            StockAfter = material.Stock,
            Reference = reference,
            Status = "Posted"
        };
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();

        return new GoodsMovementResult
        {
            Success = true,
            Message = $"Goods receipt posted for {quantity} {materialName}",
            MovementId = movement.Id,
            QuantityPosted = quantity
        };
    }

    public async Task<GoodsMovementResult> PostTransferAsync(string materialName, decimal quantity, string fromLocation, string toLocation, string userId)
    {
        var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == materialName)
            ?? throw new InvalidOperationException($"Material {materialName} not found");

        if (material.Stock < quantity)
            throw new InvalidOperationException($"Insufficient stock for transfer of {materialName}");

        var stockBefore = material.Stock;
        material.Stock -= quantity;
        material.UpdatedAt = DateTime.UtcNow;

        var issueMovement = new StockMovementEntity
        {
            DocumentNumber = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6]}",
            MaterialName = materialName,
            MovementType = "TRF-OUT",
            Quantity = quantity,
            StockBefore = stockBefore,
            StockAfter = material.Stock,
            Reference = $"Transfer from {fromLocation} to {toLocation}",
            Status = "Posted"
        };

        material.Stock += quantity;
        var receiptMovement = new StockMovementEntity
        {
            DocumentNumber = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6]}",
            MaterialName = materialName,
            MovementType = "TRF-IN",
            Quantity = quantity,
            StockBefore = material.Stock - quantity,
            StockAfter = material.Stock,
            Reference = $"Transfer from {fromLocation} to {toLocation}",
            Status = "Posted"
        };

        _db.StockMovements.AddRange(issueMovement, receiptMovement);
        await _db.SaveChangesAsync();

        return new GoodsMovementResult
        {
            Success = true,
            Message = $"Transfer posted: {quantity} {materialName} from {fromLocation} to {toLocation}",
            MovementId = issueMovement.Id,
            QuantityPosted = quantity
        };
    }

    public async Task<List<StockMovementEntity>> GetMaterialHistoryAsync(string materialName, int days = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await _db.StockMovements
            .Where(m => m.MaterialName == materialName && m.CreatedAt >= cutoff)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<BackflushResult> ExecuteBackflushAsync(Guid productionOrderId, string userId)
    {
        var order = await _db.ProductionOrders.FindAsync(productionOrderId)
            ?? throw new InvalidOperationException("Production order not found");

        var components = await _db.BillOfMaterials
            .Where(b => b.ProductName == order.ProductName && b.Status == "Active")
            .ToListAsync();

        var result = new BackflushResult();

        foreach (var comp in components)
        {
            var componentQty = order.Quantity * comp.Quantity;
            try
            {
                var movement = await PostGoodsIssueAsync(
                    comp.ComponentName, componentQty,
                    "BACKFLUSH", order.OrderNumber, "GI", userId);
                result.Movements.Add(movement);
                result.ComponentsPosted++;
            }
            catch (InvalidOperationException ex)
            {
                result.Movements.Add(new GoodsMovementResult
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        result.Success = result.ComponentsPosted == components.Count;
        result.Message = result.Success
            ? $"Backflush completed: {result.ComponentsPosted} components issued"
            : $"Backflush partial: {result.ComponentsPosted}/{components.Count} components issued";

        return result;
    }
}
