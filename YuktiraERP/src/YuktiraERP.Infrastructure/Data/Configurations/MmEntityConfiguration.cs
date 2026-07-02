using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class MaterialMasterEntityConfiguration : IEntityTypeConfiguration<MaterialMasterEntity>
{
    public void Configure(EntityTypeBuilder<MaterialMasterEntity> builder)
    {
        builder.ToTable("material_masters", "yuktira_mm");
        builder.Property(e => e.Price).HasColumnType("decimal(18,2)");
    }
}

public class VendorEntityConfiguration : IEntityTypeConfiguration<VendorEntity>
{
    public void Configure(EntityTypeBuilder<VendorEntity> builder)
    {
        builder.ToTable("vendor_masters", "yuktira_mm");
    }
}

public class PurchaseRequisitionEntityConfiguration : IEntityTypeConfiguration<PurchaseRequisitionEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisitionEntity> builder)
    {
        builder.ToTable("purchase_requisitions", "yuktira_mm");
    }
}

public class PurchaseOrderEntityConfiguration : IEntityTypeConfiguration<PurchaseOrderEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderEntity> builder)
    {
        builder.ToTable("purchase_orders", "yuktira_mm");
    }
}

public class GoodsReceiptEntityConfiguration : IEntityTypeConfiguration<GoodsReceiptEntity>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptEntity> builder)
    {
        builder.ToTable("goods_receipts", "yuktira_mm");
    }
}

public class StockItemEntityConfiguration : IEntityTypeConfiguration<StockItemEntity>
{
    public void Configure(EntityTypeBuilder<StockItemEntity> builder)
    {
        builder.ToTable("stock", "yuktira_mm");
    }
}

public class InvoiceVerificationEntityConfiguration : IEntityTypeConfiguration<InvoiceVerificationEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceVerificationEntity> builder)
    {
        builder.ToTable("invoice_verifications", "yuktira_mm");
    }
}
