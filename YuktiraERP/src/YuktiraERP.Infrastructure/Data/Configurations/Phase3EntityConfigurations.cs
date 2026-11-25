using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Domain.Inventory;
using YuktiraERP.Core.Domain.MasterData;
using YuktiraERP.Core.Domain.Procurement;
using YuktiraERP.Core.Domain.Sales;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public static class Phase3EntityConfigurations
{
    public static void ApplyPhase3Configurations(this ModelBuilder modelBuilder)
    {
        // Document Flow & Cross-Cutting Common Entities
        modelBuilder.Entity<DocumentReference>(b =>
        {
            b.ToTable("document_references", "common");
            b.HasKey(e => e.Id);
            b.Property(e => e.SourceDocType).HasMaxLength(64).IsRequired();
            b.Property(e => e.TargetDocType).HasMaxLength(64).IsRequired();
            b.HasIndex(e => new { e.SourceDocType, e.SourceDocId });
            b.HasIndex(e => new { e.TargetDocType, e.TargetDocId });
        });

        // 1. Schema: masterdata
        modelBuilder.Entity<Company>(b =>
        {
            b.ToTable("companies", "masterdata");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(32).IsRequired();
            b.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<Branch>(b =>
        {
            b.ToTable("branches", "masterdata");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(32).IsRequired();
            b.HasIndex(e => new { e.CompanyId, e.Code }).IsUnique();
        });

        modelBuilder.Entity<Currency>(b =>
        {
            b.ToTable("currencies", "masterdata");
            b.HasKey(e => e.Code);
            b.Property(e => e.Code).HasMaxLength(3);
        });

        modelBuilder.Entity<CurrencyExchangeRate>(b =>
        {
            b.ToTable("currency_exchange_rates", "masterdata");
            b.HasKey(e => e.Id);
            b.Property(e => e.FromCurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(e => e.ToCurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(e => e.Rate).HasPrecision(18, 6);
        });

        modelBuilder.Entity<MasterItem>(b =>
        {
            b.ToTable("items", "masterdata");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(64).IsRequired();
            b.Property(e => e.Name).HasMaxLength(256).IsRequired();
            b.Property(e => e.BaseUomCode).HasMaxLength(16).IsRequired();
            b.Property(e => e.StandardCost).HasPrecision(18, 4);
            b.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<UomDefinition>(b =>
        {
            b.ToTable("uoms", "masterdata");
            b.HasKey(e => e.Code);
            b.Property(e => e.Code).HasMaxLength(16);
        });

        modelBuilder.Entity<UomConversion>(b =>
        {
            b.ToTable("uom_conversions", "masterdata");
            b.HasKey(e => e.Id);
            b.Property(e => e.FromUomCode).HasMaxLength(16).IsRequired();
            b.Property(e => e.ToUomCode).HasMaxLength(16).IsRequired();
            b.Property(e => e.Multiplier).HasPrecision(18, 6);
        });

        modelBuilder.Entity<Warehouse>(b =>
        {
            b.ToTable("warehouses", "masterdata");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(32).IsRequired();
            b.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<WarehouseBin>(b =>
        {
            b.ToTable("warehouse_bins", "masterdata");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(64).IsRequired();
            b.HasIndex(e => new { e.WarehouseId, e.Code }).IsUnique();
        });

        modelBuilder.Entity<BusinessPartner>(b =>
        {
            b.ToTable("business_partners", "masterdata");
            b.HasKey(e => e.Id);
            b.Property(e => e.Code).HasMaxLength(64).IsRequired();
            b.Property(e => e.CreditLimit).HasPrecision(18, 2);
            b.Property(e => e.CurrentBalance).HasPrecision(18, 2);
            b.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<NumberingSeries>(b =>
        {
            b.ToTable("numbering_series", "masterdata");
            b.HasKey(e => e.Id);
            b.Property(e => e.DocumentType).HasMaxLength(64).IsRequired();
            b.HasIndex(e => new { e.DocumentType, e.FiscalYear }).IsUnique();
        });

        // 2. Schema: inventory
        modelBuilder.Entity<ItemBatch>(b =>
        {
            b.ToTable("batches", "inventory");
            b.HasKey(e => e.Id);
            b.Property(e => e.BatchNumber).HasMaxLength(64).IsRequired();
            b.Property(e => e.QtyOnHand).HasPrecision(18, 4);
            b.Property(e => e.QtyReserved).HasPrecision(18, 4);
            b.Property(e => e.UnitCost).HasPrecision(18, 4);
            b.HasIndex(e => new { e.ItemId, e.WarehouseId, e.BatchNumber }).IsUnique();
            b.HasIndex(e => e.IsHidden);
        });

        modelBuilder.Entity<ItemSerial>(b =>
        {
            b.ToTable("serials", "inventory");
            b.HasKey(e => e.Id);
            b.Property(e => e.SerialNumber).HasMaxLength(128).IsRequired();
            b.Property(e => e.UnitCost).HasPrecision(18, 4);
            b.HasIndex(e => new { e.ItemId, e.SerialNumber }).IsUnique();
        });

        modelBuilder.Entity<StockMovement>(b =>
        {
            b.ToTable("stock_movements", "inventory");
            b.HasKey(e => e.Id);
            b.Property(e => e.Quantity).HasPrecision(18, 4);
            b.Property(e => e.UnitCost).HasPrecision(18, 4);
            b.Property(e => e.TotalValue).HasPrecision(18, 4);
            b.HasIndex(e => new { e.ItemId, e.WarehouseId, e.MovementDateUtc });
        });

        modelBuilder.Entity<InventoryValuation>(b =>
        {
            b.ToTable("valuations", "inventory");
            b.HasKey(e => e.Id);
            b.Property(e => e.OnHandQty).HasPrecision(18, 4);
            b.Property(e => e.TotalInventoryValue).HasPrecision(18, 4);
            b.Property(e => e.MovingAverageCost).HasPrecision(18, 6);
            b.HasIndex(e => new { e.ItemId, e.WarehouseId }).IsUnique();
        });

        // 3. Schema: sales
        modelBuilder.Entity<SalesQuotation>(b =>
        {
            b.ToTable("quotations", "sales");
            b.HasKey(e => e.Id);
            b.Property(e => e.QuotationNumber).HasMaxLength(64).IsRequired();
            b.Property(e => e.TotalAmount).HasPrecision(18, 2);
            b.HasIndex(e => e.QuotationNumber).IsUnique();
        });

        modelBuilder.Entity<SalesQuotationLine>(b =>
        {
            b.ToTable("quotation_lines", "sales");
            b.HasKey(e => e.Id);
            b.Property(e => e.Quantity).HasPrecision(18, 4);
            b.Property(e => e.UnitPrice).HasPrecision(18, 4);
            b.Property(e => e.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SalesOrder>(b =>
        {
            b.ToTable("orders", "sales");
            b.HasKey(e => e.Id);
            b.Property(e => e.OrderNumber).HasMaxLength(64).IsRequired();
            b.Property(e => e.TotalAmount).HasPrecision(18, 2);
            b.HasIndex(e => e.OrderNumber).IsUnique();
        });

        modelBuilder.Entity<SalesOrderLine>(b =>
        {
            b.ToTable("order_lines", "sales");
            b.HasKey(e => e.Id);
            b.Property(e => e.OrderedQty).HasPrecision(18, 4);
            b.Property(e => e.DeliveredQty).HasPrecision(18, 4);
            b.Property(e => e.InvoicedQty).HasPrecision(18, 4);
            b.Property(e => e.UnitPrice).HasPrecision(18, 4);
            b.Property(e => e.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SalesDelivery>(b =>
        {
            b.ToTable("deliveries", "sales");
            b.HasKey(e => e.Id);
            b.Property(e => e.DeliveryNumber).HasMaxLength(64).IsRequired();
            b.HasIndex(e => e.DeliveryNumber).IsUnique();
        });

        modelBuilder.Entity<SalesDeliveryLine>(b =>
        {
            b.ToTable("delivery_lines", "sales");
            b.HasKey(e => e.Id);
            b.Property(e => e.DeliveredQty).HasPrecision(18, 4);
        });

        modelBuilder.Entity<SalesInvoice>(b =>
        {
            b.ToTable("invoices", "sales");
            b.HasKey(e => e.Id);
            b.Property(e => e.InvoiceNumber).HasMaxLength(64).IsRequired();
            b.Property(e => e.TotalAmount).HasPrecision(18, 2);
            b.HasIndex(e => e.InvoiceNumber).IsUnique();
        });

        modelBuilder.Entity<SalesInvoiceLine>(b =>
        {
            b.ToTable("invoice_lines", "sales");
            b.HasKey(e => e.Id);
            b.Property(e => e.InvoicedQty).HasPrecision(18, 4);
            b.Property(e => e.UnitPrice).HasPrecision(18, 4);
            b.Property(e => e.LineTotal).HasPrecision(18, 2);
        });

        // 4. Schema: procurement
        modelBuilder.Entity<PurchaseOrder>(b =>
        {
            b.ToTable("orders", "procurement");
            b.HasKey(e => e.Id);
            b.Property(e => e.PoNumber).HasMaxLength(64).IsRequired();
            b.Property(e => e.TotalAmount).HasPrecision(18, 2);
            b.HasIndex(e => e.PoNumber).IsUnique();
        });

        modelBuilder.Entity<PurchaseOrderLine>(b =>
        {
            b.ToTable("order_lines", "procurement");
            b.HasKey(e => e.Id);
            b.Property(e => e.OrderedQty).HasPrecision(18, 4);
            b.Property(e => e.ReceivedQty).HasPrecision(18, 4);
            b.Property(e => e.InvoicedQty).HasPrecision(18, 4);
            b.Property(e => e.UnitPrice).HasPrecision(18, 4);
            b.Property(e => e.LineTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<GoodsReceiptPo>(b =>
        {
            b.ToTable("goods_receipts", "procurement");
            b.HasKey(e => e.Id);
            b.Property(e => e.GrpoNumber).HasMaxLength(64).IsRequired();
            b.HasIndex(e => e.GrpoNumber).IsUnique();
        });

        modelBuilder.Entity<GoodsReceiptPoLine>(b =>
        {
            b.ToTable("goods_receipt_lines", "procurement");
            b.HasKey(e => e.Id);
            b.Property(e => e.ReceivedQty).HasPrecision(18, 4);
            b.Property(e => e.UnitCost).HasPrecision(18, 4);
        });

        modelBuilder.Entity<ApInvoice>(b =>
        {
            b.ToTable("invoices", "procurement");
            b.HasKey(e => e.Id);
            b.Property(e => e.InvoiceNumber).HasMaxLength(64).IsRequired();
            b.Property(e => e.TotalAmount).HasPrecision(18, 2);
            b.HasIndex(e => e.InvoiceNumber).IsUnique();
        });

        modelBuilder.Entity<ApInvoiceLine>(b =>
        {
            b.ToTable("invoice_lines", "procurement");
            b.HasKey(e => e.Id);
            b.Property(e => e.InvoicedQty).HasPrecision(18, 4);
            b.Property(e => e.UnitPrice).HasPrecision(18, 4);
            b.Property(e => e.LineTotal).HasPrecision(18, 2);
        });
    }
}
