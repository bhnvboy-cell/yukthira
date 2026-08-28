using HotChocolate.Types;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace YuktiraERP.Api.GraphQL;

// ══════════════════════════════════════════════════════════════════════════════
// GraphQL Types
// ══════════════════════════════════════════════════════════════════════════════

public class MaterialMasterType : ObjectType<MaterialMasterEntity>
{
    protected override void Configure(IObjectTypeDescriptor<MaterialMasterEntity> descriptor)
    {
        descriptor.Name("Material");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.Code).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.Name).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.Type).Type<StringType>();
        descriptor.Field(f => f.UOM).Type<StringType>();
        descriptor.Field(f => f.Stock).Type<DecimalType>();
        descriptor.Field(f => f.Price).Type<DecimalType>();
        descriptor.Field(f => f.Status).Type<StringType>();
    }
}

public class CustomerType : ObjectType<CustomerEntity>
{
    protected override void Configure(IObjectTypeDescriptor<CustomerEntity> descriptor)
    {
        descriptor.Name("Customer");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.Code).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.Name).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.CreditLimit).Type<DecimalType>();
        descriptor.Field(f => f.PaymentTerms).Type<StringType>();
        descriptor.Field(f => f.Status).Type<StringType>();
    }
}

public class VendorType : ObjectType<VendorEntity>
{
    protected override void Configure(IObjectTypeDescriptor<VendorEntity> descriptor)
    {
        descriptor.Name("Vendor");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.Code).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.Name).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.TaxId).Type<StringType>();
        descriptor.Field(f => f.PaymentTerms).Type<StringType>();
        descriptor.Field(f => f.Status).Type<StringType>();
    }
}

public class SalesOrderType : ObjectType<SalesOrderEntity>
{
    protected override void Configure(IObjectTypeDescriptor<SalesOrderEntity> descriptor)
    {
        descriptor.Name("SalesOrder");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.OrderNumber).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.CustomerName).Type<StringType>();
        descriptor.Field(f => f.OrderDate).Type<DateTimeType>();
        descriptor.Field(f => f.Amount).Type<DecimalType>();
        descriptor.Field(f => f.Status).Type<StringType>();
        descriptor.Field(f => f.Lines).Type<ListType<SalesOrderLineType>>();
    }
}

public class SalesOrderLineType : ObjectType<SalesOrderLineEntity>
{
    protected override void Configure(IObjectTypeDescriptor<SalesOrderLineEntity> descriptor)
    {
        descriptor.Name("SalesOrderLine");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.MaterialName).Type<StringType>();
        descriptor.Field(f => f.Quantity).Type<DecimalType>();
        descriptor.Field(f => f.UnitPrice).Type<DecimalType>();
        descriptor.Field(f => f.TotalPrice).Type<DecimalType>();
    }
}

public class PurchaseOrderType : ObjectType<PurchaseOrderEntity>
{
    protected override void Configure(IObjectTypeDescriptor<PurchaseOrderEntity> descriptor)
    {
        descriptor.Name("PurchaseOrder");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.PoNumber).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.VendorName).Type<StringType>();
        descriptor.Field(f => f.Date).Type<DateTimeType>();
        descriptor.Field(f => f.Amount).Type<DecimalType>();
        descriptor.Field(f => f.Status).Type<StringType>();
        descriptor.Field(f => f.Items).Type<ListType<PurchaseOrderItemType>>();
    }
}

public class PurchaseOrderItemType : ObjectType<PurchaseOrderItemEntity>
{
    protected override void Configure(IObjectTypeDescriptor<PurchaseOrderItemEntity> descriptor)
    {
        descriptor.Name("PurchaseOrderItem");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.MaterialName).Type<StringType>();
        descriptor.Field(f => f.Quantity).Type<DecimalType>();
        descriptor.Field(f => f.UnitPrice).Type<DecimalType>();
        descriptor.Field(f => f.TotalPrice).Type<DecimalType>();
    }
}

public class ProductionOrderType : ObjectType<ProductionOrderEntity>
{
    protected override void Configure(IObjectTypeDescriptor<ProductionOrderEntity> descriptor)
    {
        descriptor.Name("ProductionOrder");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.OrderNumber).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.ProductName).Type<StringType>();
        descriptor.Field(f => f.Quantity).Type<DecimalType>();
        descriptor.Field(f => f.StartDate).Type<DateTimeType>();
        descriptor.Field(f => f.EndDate).Type<DateTimeType>();
        descriptor.Field(f => f.Status).Type<StringType>();
        descriptor.Field(f => f.ActualCost).Type<DecimalType>();
        descriptor.Field(f => f.PlannedCost).Type<DecimalType>();
    }
}

public class StockItemType : ObjectType<StockItemEntity>
{
    protected override void Configure(IObjectTypeDescriptor<StockItemEntity> descriptor)
    {
        descriptor.Name("StockItem");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.MaterialName).Type<StringType>();
        descriptor.Field(f => f.Quantity).Type<DecimalType>();
        descriptor.Field(f => f.UOM).Type<StringType>();
        descriptor.Field(f => f.Value).Type<DecimalType>();
        descriptor.Field(f => f.Bin).Type<StringType>();
        descriptor.Field(f => f.Lot).Type<StringType>();
    }
}

public class BatchType : ObjectType<BatchEntity>
{
    protected override void Configure(IObjectTypeDescriptor<BatchEntity> descriptor)
    {
        descriptor.Name("Batch");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.BatchNumber).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.MaterialName).Type<StringType>();
        descriptor.Field(f => f.Quantity).Type<DecimalType>();
        descriptor.Field(f => f.Status).Type<StringType>();
        descriptor.Field(f => f.ManufacturingDate).Type<DateTimeType>();
        descriptor.Field(f => f.ExpiryDate).Type<DateTimeType>();
    }
}

public class QualityNotificationType : ObjectType<QualityNotificationEntity>
{
    protected override void Configure(IObjectTypeDescriptor<QualityNotificationEntity> descriptor)
    {
        descriptor.Name("QualityNotification");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.NotificationNumber).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.NotificationType).Type<StringType>();
        descriptor.Field(f => f.Description).Type<StringType>();
        descriptor.Field(f => f.DefectCode).Type<StringType>();
        descriptor.Field(f => f.Priority).Type<StringType>();
        descriptor.Field(f => f.Status).Type<StringType>();
    }
}

public class UniversalJournalType : ObjectType<UniversalJournalEntity>
{
    protected override void Configure(IObjectTypeDescriptor<UniversalJournalEntity> descriptor)
    {
        descriptor.Name("JournalEntry");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.DocumentNumber).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.AccountCode).Type<StringType>();
        descriptor.Field(f => f.AccountName).Type<StringType>();
        descriptor.Field(f => f.DebitAmount).Type<DecimalType>();
        descriptor.Field(f => f.CreditAmount).Type<DecimalType>();
        descriptor.Field(f => f.CostCenter).Type<StringType>();
        descriptor.Field(f => f.ProfitCenter).Type<StringType>();
        descriptor.Field(f => f.PostingDate).Type<DateTimeType>();
        descriptor.Field(f => f.Status).Type<StringType>();
    }
}

public class StockMovementType : ObjectType<StockMovementEntity>
{
    protected override void Configure(IObjectTypeDescriptor<StockMovementEntity> descriptor)
    {
        descriptor.Name("StockMovement");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.DocumentNumber).Type<StringType>();
        descriptor.Field(f => f.MaterialName).Type<StringType>();
        descriptor.Field(f => f.MovementType).Type<StringType>();
        descriptor.Field(f => f.Quantity).Type<DecimalType>();
        descriptor.Field(f => f.StockBefore).Type<DecimalType>();
        descriptor.Field(f => f.StockAfter).Type<DecimalType>();
        descriptor.Field(f => f.Reference).Type<StringType>();
        descriptor.Field(f => f.Status).Type<StringType>();
    }
}

public class InspectionLotType : ObjectType<InspectionLotEntity>
{
    protected override void Configure(IObjectTypeDescriptor<InspectionLotEntity> descriptor)
    {
        descriptor.Name("InspectionLot");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.LotNumber).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.MaterialName).Type<StringType>();
        descriptor.Field(f => f.Inspected).Type<IntType>();
        descriptor.Field(f => f.Passed).Type<IntType>();
        descriptor.Field(f => f.Failed).Type<IntType>();
        descriptor.Field(f => f.Status).Type<StringType>();
    }
}

public class MaintenanceOrderType : ObjectType<MaintenanceOrderEntity>
{
    protected override void Configure(IObjectTypeDescriptor<MaintenanceOrderEntity> descriptor)
    {
        descriptor.Name("MaintenanceOrder");
        descriptor.Field(f => f.Id).Type<IdType>();
        descriptor.Field(f => f.OrderNumber).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.EquipmentCode).Type<StringType>();
        descriptor.Field(f => f.Description).Type<StringType>();
        descriptor.Field(f => f.Priority).Type<StringType>();
        descriptor.Field(f => f.Status).Type<StringType>();
        descriptor.Field(f => f.Cost).Type<DecimalType>();
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GraphQL Dashboard Types
// ══════════════════════════════════════════════════════════════════════════════

public class DashboardKpiType
{
    public string Name { get; set; } = "";
    public decimal Value { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal ChangePercent { get; set; }
    public string Trend { get; set; } = "";
    public string Unit { get; set; } = "";
}

public class InventorySummaryType
{
    public int TotalMaterials { get; set; }
    public decimal TotalStockValue { get; set; }
    public int LowStockCount { get; set; }
    public int OverStockCount { get; set; }
    public List<StockItemEntity> LowStockItems { get; set; } = new();
}

public class SalesSummaryType
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
}

public class ProductionSummaryType
{
    public int TotalOrders { get; set; }
    public int PlannedOrders { get; set; }
    public int InProgressOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalPlannedCost { get; set; }
    public decimal TotalActualCost { get; set; }
    public decimal CostVariance { get; set; }
}

public class QualitySummaryType
{
    public int TotalLots { get; set; }
    public int PendingInspection { get; set; }
    public int PassedLots { get; set; }
    public int FailedLots { get; set; }
    public decimal PassRate { get; set; }
}

public class ProcurementSummaryType
{
    public int TotalPOs { get; set; }
    public decimal TotalValue { get; set; }
    public int PendingGR { get; set; }
    public int PendingIR { get; set; }
}

public class FinancialSummaryType
{
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal NetBalance { get; set; }
    public int JournalEntries { get; set; }
    public int PostedEntries { get; set; }
}

public class DashboardSummaryType
{
    public List<DashboardKpiType> Kpis { get; set; } = new();
    public InventorySummaryType Inventory { get; set; } = new();
    public SalesSummaryType Sales { get; set; } = new();
    public ProductionSummaryType Production { get; set; } = new();
    public QualitySummaryType Quality { get; set; } = new();
    public ProcurementSummaryType Procurement { get; set; } = new();
    public FinancialSummaryType Financial { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class MaterialStockAlertType
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal MinStock { get; set; }
    public decimal MaxStock { get; set; }
    public string AlertLevel { get; set; } = "";
}
