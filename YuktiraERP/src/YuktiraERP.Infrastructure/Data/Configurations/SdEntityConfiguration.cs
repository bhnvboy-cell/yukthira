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
