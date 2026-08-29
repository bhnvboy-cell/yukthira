using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class ProductionPlanEntityConfiguration : IEntityTypeConfiguration<ProductionPlanEntity>
{
    public void Configure(EntityTypeBuilder<ProductionPlanEntity> builder)
    {
        builder.ToTable("production_plans", "yuktira_pp");
    }
}

public class BillOfMaterialEntityConfiguration : IEntityTypeConfiguration<BillOfMaterialEntity>
{
    public void Configure(EntityTypeBuilder<BillOfMaterialEntity> builder)
    {
        builder.ToTable("bill_of_materials", "yuktira_pp");
    }
}

public class ProductionRoutingEntityConfiguration : IEntityTypeConfiguration<ProductionRoutingEntity>
{
    public void Configure(EntityTypeBuilder<ProductionRoutingEntity> builder)
    {
        builder.ToTable("production_routings", "yuktira_pp");
    }
}

public class WorkCenterEntityConfiguration : IEntityTypeConfiguration<WorkCenterEntity>
{
    public void Configure(EntityTypeBuilder<WorkCenterEntity> builder)
    {
        builder.ToTable("work_centers", "yuktira_pp");
    }
}

public class ProductionOrderEntityConfiguration : IEntityTypeConfiguration<ProductionOrderEntity>
{
    public void Configure(EntityTypeBuilder<ProductionOrderEntity> builder)
    {
        builder.ToTable("production_orders", "yuktira_pp");
    }
}

public class OrderConfirmationEntityConfiguration : IEntityTypeConfiguration<OrderConfirmationEntity>
{
    public void Configure(EntityTypeBuilder<OrderConfirmationEntity> builder)
    {
        builder.ToTable("OrderConfirmations", "yuktira_pp");
        builder.Property(e => e.YieldQuantity).HasColumnType("decimal(18,4)");
        builder.Property(e => e.ScrapQuantity).HasColumnType("decimal(18,4)");
        builder.Property(e => e.ActualSetupTime).HasColumnType("decimal(18,4)");
        builder.Property(e => e.ActualMachineTime).HasColumnType("decimal(18,4)");
        builder.Property(e => e.ActualLaborTime).HasColumnType("decimal(18,4)");
    }
}
