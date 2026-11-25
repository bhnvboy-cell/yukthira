using System;
using System.Collections.Generic;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Domain.Planning;

public class WorkCenterMaster : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty; // e.g. "WC-CNC-01", "WC-ASSY-LINE-2"
    public string Name { get; set; } = string.Empty;
    public decimal DailyCapacityHours { get; set; } = 8.0m;
    public decimal StandardHourlyRate { get; set; } = 50.0m;
    public decimal EfficiencyPercent { get; set; } = 100.0m;
    public bool IsActive { get; set; } = true;
}

public class BillOfMaterialHeader : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FinishedGoodItemId { get; set; }
    public string BomCode { get; set; } = string.Empty; // e.g. "BOM-SKU-FG01-V1"
    public int Version { get; set; } = 1;
    public decimal BaseQuantity { get; set; } = 1.0m; // Output quantity
    public string UomCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public ICollection<BillOfMaterialComponent> Components { get; set; } = new List<BillOfMaterialComponent>();
}

public class BillOfMaterialComponent : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BomHeaderId { get; set; }
    public int LineNumber { get; set; }
    public Guid ComponentItemId { get; set; }
    public string ComponentItemCode { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal ScrapPercent { get; set; } = 0m;
    public string UomCode { get; set; } = string.Empty;
    public bool IsPhantom { get; set; } = false; // Explodes automatically in MRP
}

public class ManufacturingRouting : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public string RoutingCode { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public ICollection<RoutingOperationStep> Operations { get; set; } = new List<RoutingOperationStep>();
}

public class RoutingOperationStep : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoutingId { get; set; }
    public int OperationSequence { get; set; } // 10, 20, 30
    public Guid WorkCenterId { get; set; }
    public string OperationDescription { get; set; } = string.Empty;
    public decimal SetupTimeMinutes { get; set; } = 0m;
    public decimal RunTimeMinutesPerUnit { get; set; } = 0m;
}

public enum PlannedOrderType
{
    Production = 1,
    Purchase = 2
}

public class PlannedOrder : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public PlannedOrderType OrderType { get; set; }
    public decimal Quantity { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string DemandSource { get; set; } = "MRP_RUN"; // e.g. "SalesOrder:SO-100", "Forecast"
    public bool IsFirm { get; set; } = false;
}

public class MrpDemandItem
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public decimal GrossRequirementQty { get; set; }
    public DateOnly RequirementDate { get; set; }
    public string SourceReference { get; set; } = string.Empty;
}

public class MrpSupplyItem
{
    public Guid ItemId { get; set; }
    public decimal OnHandQty { get; set; }
    public decimal ScheduledReceiptsQty { get; set; } // Open PO or open Production Orders
}
