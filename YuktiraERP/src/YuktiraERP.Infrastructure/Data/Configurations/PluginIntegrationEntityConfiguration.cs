using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class PluginEntityConfiguration : IEntityTypeConfiguration<PluginEntity>
{
    public void Configure(EntityTypeBuilder<PluginEntity> builder)
    {
        builder.ToTable("plugins", "yuktira_plugin");
    }
}

public class PluginSettingEntityConfiguration : IEntityTypeConfiguration<PluginSettingEntity>
{
    public void Configure(EntityTypeBuilder<PluginSettingEntity> builder)
    {
        builder.ToTable("plugin_settings", "yuktira_plugin");
    }
}

public class PluginTenantPermissionEntityConfiguration : IEntityTypeConfiguration<PluginTenantPermissionEntity>
{
    public void Configure(EntityTypeBuilder<PluginTenantPermissionEntity> builder)
    {
        builder.ToTable("plugin_tenant", "yuktira_plugin");
    }
}

public class NumberRangeDefinitionEntityConfiguration : IEntityTypeConfiguration<NumberRangeDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<NumberRangeDefinitionEntity> builder)
    {
        builder.ToTable("number_range_definitions", "yuktira_numberrange");
    }
}

public class WebhookEntityConfiguration : IEntityTypeConfiguration<WebhookEntity>
{
    public void Configure(EntityTypeBuilder<WebhookEntity> builder)
    {
        builder.ToTable("webhooks", "yuktira_integration");
    }
}

public class EdiTradingPartnerEntityConfiguration : IEntityTypeConfiguration<EdiTradingPartnerEntity>
{
    public void Configure(EntityTypeBuilder<EdiTradingPartnerEntity> builder)
    {
        builder.ToTable("edi_trading_partners", "yuktira_integration");
    }
}

public class EdiAcknowledgmentEntityConfiguration : IEntityTypeConfiguration<EdiAcknowledgmentEntity>
{
    public void Configure(EntityTypeBuilder<EdiAcknowledgmentEntity> builder)
    {
        builder.ToTable("edi_acknowledgment_logs", "yuktira_integration");
    }
}

public class WebhookDeliveryLogEntityConfiguration : IEntityTypeConfiguration<WebhookDeliveryLogEntity>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLogEntity> builder)
    {
        builder.ToTable("webhook_delivery_logs", "yuktira_integration");
    }
}

public class ApiClientEntityConfiguration : IEntityTypeConfiguration<ApiClientEntity>
{
    public void Configure(EntityTypeBuilder<ApiClientEntity> builder)
    {
        builder.ToTable("api_clients", "yuktira_integration");
    }
}

public class IntegrationQueueEntityConfiguration : IEntityTypeConfiguration<IntegrationQueueEntity>
{
    public void Configure(EntityTypeBuilder<IntegrationQueueEntity> builder)
    {
        builder.ToTable("integration_queue", "yuktira_integration");
    }
}

public class IntegrationDeadLetterEntityConfiguration : IEntityTypeConfiguration<IntegrationDeadLetterEntity>
{
    public void Configure(EntityTypeBuilder<IntegrationDeadLetterEntity> builder)
    {
        builder.ToTable("integration_dead_letter", "yuktira_integration");
    }
}

public class IntegrationConnectionEntityConfiguration : IEntityTypeConfiguration<IntegrationConnectionEntity>
{
    public void Configure(EntityTypeBuilder<IntegrationConnectionEntity> builder)
    {
        builder.ToTable("integration_connections", "yuktira_integration");
    }
}

public class SyncJobEntityConfiguration : IEntityTypeConfiguration<SyncJobEntity>
{
    public void Configure(EntityTypeBuilder<SyncJobEntity> builder)
    {
        builder.ToTable("sync_jobs", "yuktira_integration");
    }
}

public class SyncLogEntityConfiguration : IEntityTypeConfiguration<SyncLogEntity>
{
    public void Configure(EntityTypeBuilder<SyncLogEntity> builder)
    {
        builder.ToTable("sync_logs", "yuktira_integration");
    }
}

public class MappingRuleEntityConfiguration : IEntityTypeConfiguration<MappingRuleEntity>
{
    public void Configure(EntityTypeBuilder<MappingRuleEntity> builder)
    {
        builder.ToTable("mapping_rules", "yuktira_integration");
    }
}

public class MrpRunHistoryEntityConfiguration : IEntityTypeConfiguration<MrpRunHistoryEntity>
{
    public void Configure(EntityTypeBuilder<MrpRunHistoryEntity> builder)
    {
        builder.ToTable("mrp_run_history", "yuktira_mrp");
        builder.Property(e => e.DurationMs).HasColumnType("bigint");
    }
}

public class MrpExceptionMessageEntityConfiguration : IEntityTypeConfiguration<MrpExceptionMessageEntity>
{
    public void Configure(EntityTypeBuilder<MrpExceptionMessageEntity> builder)
    {
        builder.ToTable("mrp_exception_message", "yuktira_mrp");
    }
}

public class PlantEntityConfiguration : IEntityTypeConfiguration<PlantEntity>
{
    public void Configure(EntityTypeBuilder<PlantEntity> builder)
    {
        builder.ToTable("plant", "yuktira_mrp");
    }
}

public class VendorLeadTimeEntityConfiguration : IEntityTypeConfiguration<VendorLeadTimeEntity>
{
    public void Configure(EntityTypeBuilder<VendorLeadTimeEntity> builder)
    {
        builder.ToTable("vendor_lead_time", "yuktira_mrp");
        builder.Property(e => e.Reliability).HasColumnType("decimal(5,2)");
    }
}

public class MrpCapacityLevelEntityConfiguration : IEntityTypeConfiguration<MrpCapacityLevelEntity>
{
    public void Configure(EntityTypeBuilder<MrpCapacityLevelEntity> builder)
    {
        builder.ToTable("mrp_capacity_level", "yuktira_mrp");
        builder.Property(e => e.AvailableHours).HasColumnType("decimal(18,2)");
        builder.Property(e => e.RequiredHours).HasColumnType("decimal(18,2)");
        builder.Property(e => e.LoadPercent).HasColumnType("decimal(5,1)");
    }
}

public class TransactionCodeEntityConfiguration : IEntityTypeConfiguration<TransactionCodeEntity>
{
    public void Configure(EntityTypeBuilder<TransactionCodeEntity> builder)
    {
        builder.ToTable("transaction_codes", "yuktira_transaction");
    }
}

public class TransactionPermissionEntityConfiguration : IEntityTypeConfiguration<TransactionPermissionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionPermissionEntity> builder)
    {
        builder.ToTable("transaction_permissions", "yuktira_transaction");
    }
}

public class TransactionLogEntityConfiguration : IEntityTypeConfiguration<TransactionLogEntity>
{
    public void Configure(EntityTypeBuilder<TransactionLogEntity> builder)
    {
        builder.ToTable("transaction_logs", "yuktira_transaction");
    }
}

public class TCodeDefinitionEntityConfiguration : IEntityTypeConfiguration<TCodeDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<TCodeDefinitionEntity> builder)
    {
        builder.ToTable("tcode_definitions", "yuktira_transaction");
    }
}

public class TCodeFieldEntityConfiguration : IEntityTypeConfiguration<TCodeFieldEntity>
{
    public void Configure(EntityTypeBuilder<TCodeFieldEntity> builder)
    {
        builder.ToTable("tcode_fields", "yuktira_transaction");
    }
}

public class TCodeDataEntityConfiguration : IEntityTypeConfiguration<TCodeDataEntity>
{
    public void Configure(EntityTypeBuilder<TCodeDataEntity> builder)
    {
        builder.ToTable("tcode_data", "yuktira_transaction");
    }
}

public class CustomizationTCodeFieldEntityConfiguration : IEntityTypeConfiguration<CustomizationTCodeFieldEntity>
{
    public void Configure(EntityTypeBuilder<CustomizationTCodeFieldEntity> builder)
    {
        builder.ToTable("customization_tcode_fields", "yuktira_customization");
    }
}

public class CustomizationTCodeLayoutEntityConfiguration : IEntityTypeConfiguration<CustomizationTCodeLayoutEntity>
{
    public void Configure(EntityTypeBuilder<CustomizationTCodeLayoutEntity> builder)
    {
        builder.ToTable("customization_tcode_layouts", "yuktira_customization");
    }
}
