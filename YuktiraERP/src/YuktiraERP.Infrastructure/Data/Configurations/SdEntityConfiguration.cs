using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class CustomerEntityConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customer_masters", "yuktira_sd");
    }
}

public class SalesOrderEntityConfiguration : IEntityTypeConfiguration<SalesOrderEntity>
{
    public void Configure(EntityTypeBuilder<SalesOrderEntity> builder)
    {
        builder.ToTable("sales_orders", "yuktira_sd");
        builder.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SalesOrderId);
    }
}

public class SalesOrderLineEntityConfiguration : IEntityTypeConfiguration<SalesOrderLineEntity>
{
    public void Configure(EntityTypeBuilder<SalesOrderLineEntity> builder)
    {
        builder.ToTable("sales_order_items", "yuktira_sd");
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(l => l.TotalPrice).HasColumnType("decimal(18,2)");
    }
}

public class PricingConditionEntityConfiguration : IEntityTypeConfiguration<PricingConditionEntity>
{
    public void Configure(EntityTypeBuilder<PricingConditionEntity> builder)
    {
        builder.ToTable("pricing_conditions", "yuktira_sd");
        builder.Property(c => c.Rate).HasColumnType("decimal(18,4)");
        builder.Property(c => c.Amount).HasColumnType("decimal(18,2)");
        builder.Property(c => c.PerUnit).HasColumnType("decimal(18,4)");
        builder.Property(c => c.PercentageValue).HasColumnType("decimal(18,4)");
    }
}

public class PricingConditionStepEntityConfiguration : IEntityTypeConfiguration<PricingConditionStepEntity>
{
    public void Configure(EntityTypeBuilder<PricingConditionStepEntity> builder)
    {
        builder.ToTable("pricing_condition_steps", "yuktira_sd");
        builder.HasOne<PricingConditionEntity>().WithMany().HasForeignKey(s => s.ConditionId);
        builder.Property(s => s.Rate).HasColumnType("decimal(18,4)");
        builder.Property(s => s.Amount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.PerUnit).HasColumnType("decimal(18,4)");
        builder.Property(s => s.PercentageValue).HasColumnType("decimal(18,4)");
    }
}

public class BillingDocumentLineEntityConfiguration : IEntityTypeConfiguration<BillingDocumentLineEntity>
{
    public void Configure(EntityTypeBuilder<BillingDocumentLineEntity> builder)
    {
        builder.ToTable("billing_document_lines", "yuktira_sd");
        builder.HasOne<BillingDocumentEntity>().WithMany().HasForeignKey(l => l.BillingDocumentId);
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(l => l.LineAmount).HasColumnType("decimal(18,2)");
        builder.Property(l => l.Discount).HasColumnType("decimal(18,2)");
        builder.Property(l => l.NetAmount).HasColumnType("decimal(18,2)");
        builder.Property(l => l.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(l => l.GrossAmount).HasColumnType("decimal(18,2)");
    }
}
