using System;
using System.Collections.Generic;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Domain.Inventory;

public class ItemBatch : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? WarehouseBinId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string? SupplierBatchNumber { get; set; }
    public DateOnly ManufacturingDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public decimal QtyOnHand { get; set; } = 0m;
    public decimal QtyReserved { get; set; } = 0m;
    public decimal AvailableQty => Math.Max(0m, QtyOnHand - QtyReserved);
    public decimal UnitCost { get; set; } = 0m;
    public bool IsLocked { get; set; } = false;
    public string? LockReason { get; set; }

    /// <summary>
    /// Requirement: zero-quantity batch auto-hiding.
    /// When true, UI and allocation queries exclude this batch record.
    /// </summary>
    public bool IsHidden { get; set; } = false;

    public void UpdateHiddenState()
    {
        IsHidden = (QtyOnHand <= 0m);
    }
}

public enum SerialStatus
{
    InStock = 1,
    Reserved = 2,
    Dispatched = 3,
    Quarantined = 4,
    Scrapped = 5
}

public class ItemSerial : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? WarehouseBinId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public SerialStatus Status { get; set; } = SerialStatus.InStock;
    public decimal UnitCost { get; set; } = 0m;
}

public enum StockMovementType
{
    GoodsReceipt = 1,
    GoodsIssue = 2,
    SalesDispatch = 3,
    CustomerReturn = 4,
    PurchaseReturn = 5,
    WarehouseTransfer = 6,
    Scrap = 7,
    CycleCountAdjustment = 8
}

public class StockMovement : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? WarehouseBinId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? SerialId { get; set; }
    public StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string? SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string? ReferenceNotes { get; set; }
    public DateTime MovementDateUtc { get; set; } = DateTime.UtcNow;
}

public class InventoryValuation : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal OnHandQty { get; set; } = 0m;
    public decimal TotalInventoryValue { get; set; } = 0m;
    public decimal MovingAverageCost { get; set; } = 0m;
    public DateTime LastCalculatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class BatchDepletionResult
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public decimal DepletedQty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ExtendedCost => DepletedQty * UnitCost;
    public decimal RemainingBatchQty { get; set; }
    public bool BatchAutoHidden { get; set; }
}
