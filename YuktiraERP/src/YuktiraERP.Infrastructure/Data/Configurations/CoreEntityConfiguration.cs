using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class AdminUserEntityConfiguration : IEntityTypeConfiguration<AdminUserEntity>
{
    public void Configure(EntityTypeBuilder<AdminUserEntity> builder)
    {
        builder.ToTable("users", "yuktira_core");
    }
}

public class TenantEntityConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public void Configure(EntityTypeBuilder<TenantEntity> builder)
    {
        builder.ToTable("tenants", "yuktira_core");
    }
}

public class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("refresh_tokens", "yuktira_core");
    }
}

public class TenantSettingEntityConfiguration : IEntityTypeConfiguration<TenantSettingEntity>
{
    public void Configure(EntityTypeBuilder<TenantSettingEntity> builder)
    {
        builder.ToTable("tenant_settings", "yuktira_core");
    }
}

public class SystemConfigEntityConfiguration : IEntityTypeConfiguration<SystemConfigEntity>
{
    public void Configure(EntityTypeBuilder<SystemConfigEntity> builder)
    {
        builder.ToTable("system_configs", "yuktira_core");
    }
}

public class ApprovalRequestEntityConfiguration : IEntityTypeConfiguration<ApprovalRequestEntity>
{
    public void Configure(EntityTypeBuilder<ApprovalRequestEntity> builder)
    {
        builder.ToTable("approval_requests", "yuktira_approval");
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
    }
}

public class ApprovalStepEntityConfiguration : IEntityTypeConfiguration<ApprovalStepEntity>
{
    public void Configure(EntityTypeBuilder<ApprovalStepEntity> builder)
    {
        builder.ToTable("approval_steps", "yuktira_approval");
    }
}

public class CustomFieldEntityConfiguration : IEntityTypeConfiguration<CustomFieldEntity>
{
    public void Configure(EntityTypeBuilder<CustomFieldEntity> builder)
    {
        builder.ToTable("custom_fields", "yuktira_customization");
    }
}

public class StockMovementEntityConfiguration : IEntityTypeConfiguration<StockMovementEntity>
{
    public void Configure(EntityTypeBuilder<StockMovementEntity> builder)
    {
        builder.ToTable("stock_movements", "yuktira_mm");
        builder.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(e => e.StockBefore).HasColumnType("decimal(18,4)");
        builder.Property(e => e.StockAfter).HasColumnType("decimal(18,4)");
        builder.Property(e => e.MaterialCode).HasColumnName("material_code");
        builder.Property(e => e.SourceBin).HasColumnName("source_bin");
        builder.Property(e => e.DestinationBin).HasColumnName("destination_bin");
        builder.Property(e => e.UOM).HasColumnName("uom");
        builder.Property(e => e.BatchNumber).HasColumnName("batch_number");
        builder.Property(e => e.PostedBy).HasColumnName("posted_by");
        builder.Property(e => e.MovementDate).HasColumnName("movement_date");
        builder.Property(e => e.MovementNumber).HasColumnName("movement_number");
    }
}

public class MessageDeliveryEntityConfiguration : IEntityTypeConfiguration<MessageDeliveryEntity>
{
    public void Configure(EntityTypeBuilder<MessageDeliveryEntity> builder)
    {
        builder.ToTable("message_deliveries", "yuktira_notification");
    }
}

public class CostAllocationRuleEntityConfiguration : IEntityTypeConfiguration<CostAllocationRuleEntity>
{
    public void Configure(EntityTypeBuilder<CostAllocationRuleEntity> builder)
    {
        builder.ToTable("cost_allocation_rules", "yuktira_fi");
    }
}

public class CostAllocationRunEntityConfiguration : IEntityTypeConfiguration<CostAllocationRunEntity>
{
    public void Configure(EntityTypeBuilder<CostAllocationRunEntity> builder)
    {
        builder.ToTable("cost_allocation_runs", "yuktira_fi");
        builder.Property(e => e.TotalAllocated).HasColumnType("decimal(18,2)");
    }
}

public class CostAllocationDetailEntityConfiguration : IEntityTypeConfiguration<CostAllocationDetailEntity>
{
    public void Configure(EntityTypeBuilder<CostAllocationDetailEntity> builder)
    {
        builder.ToTable("cost_allocation_details", "yuktira_fi");
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.Property(e => e.SharePercent).HasColumnType("decimal(5,2)");
    }
}

public class LanguageEntityConfiguration : IEntityTypeConfiguration<LanguageEntity>
{
    public void Configure(EntityTypeBuilder<LanguageEntity> builder)
    {
        builder.ToTable("languages", "yuktira_core");
    }
}

public class TranslationEntityConfiguration : IEntityTypeConfiguration<TranslationEntity>
{
    public void Configure(EntityTypeBuilder<TranslationEntity> builder)
    {
        builder.ToTable("translations", "yuktira_core");
    }
}

public class ProjectEntityConfiguration : IEntityTypeConfiguration<ProjectEntity>
{
    public void Configure(EntityTypeBuilder<ProjectEntity> builder)
    {
        builder.ToTable("projects", "yuktira_pp");
        builder.Property(e => e.Budget).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Spent).HasColumnType("decimal(18,2)");
    }
}

public class ProjectTaskEntityConfiguration : IEntityTypeConfiguration<ProjectTaskEntity>
{
    public void Configure(EntityTypeBuilder<ProjectTaskEntity> builder)
    {
        builder.ToTable("project_tasks", "yuktira_pp");
        builder.Property(e => e.PlannedHours).HasColumnType("decimal(18,4)");
        builder.Property(e => e.ActualHours).HasColumnType("decimal(18,4)");
    }
}

public class TimesheetEntryEntityConfiguration : IEntityTypeConfiguration<TimesheetEntryEntity>
{
    public void Configure(EntityTypeBuilder<TimesheetEntryEntity> builder)
    {
        builder.ToTable("timesheet_entries", "yuktira_pp");
        builder.Property(e => e.Hours).HasColumnType("decimal(18,4)");
    }
}

public class EquipmentEntityConfiguration : IEntityTypeConfiguration<EquipmentEntity>
{
    public void Configure(EntityTypeBuilder<EquipmentEntity> builder)
    {
        builder.ToTable("equipment", "yuktira_pm");
    }
}

public class MaintenancePlanEntityConfiguration : IEntityTypeConfiguration<MaintenancePlanEntity>
{
    public void Configure(EntityTypeBuilder<MaintenancePlanEntity> builder)
    {
        builder.ToTable("maintenance_plans", "yuktira_pm");
        builder.Property(e => e.EstimatedHours).HasColumnType("decimal(18,4)");
    }
}

public class MaintenanceOrderEntityConfiguration : IEntityTypeConfiguration<MaintenanceOrderEntity>
{
    public void Configure(EntityTypeBuilder<MaintenanceOrderEntity> builder)
    {
        builder.ToTable("maintenance_orders", "yuktira_pm");
        builder.Property(e => e.Cost).HasColumnType("decimal(18,2)");
    }
}
