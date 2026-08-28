using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class InspectionLotEntityConfiguration : IEntityTypeConfiguration<InspectionLotEntity>
{
    public void Configure(EntityTypeBuilder<InspectionLotEntity> builder)
    {
        builder.ToTable("inspection_lots", "yuktira_qm");
    }
}

public class InspectionPlanEntityConfiguration : IEntityTypeConfiguration<InspectionPlanEntity>
{
    public void Configure(EntityTypeBuilder<InspectionPlanEntity> builder)
    {
        builder.ToTable("inspection_plans", "yuktira_qm");
    }
}

public class InspectionResultEntityConfiguration : IEntityTypeConfiguration<InspectionResultEntity>
{
    public void Configure(EntityTypeBuilder<InspectionResultEntity> builder)
    {
        builder.ToTable("inspection_results", "yuktira_qm");
    }
}

public class UsageDecisionEntityConfiguration : IEntityTypeConfiguration<UsageDecisionEntity>
{
    public void Configure(EntityTypeBuilder<UsageDecisionEntity> builder)
    {
        builder.ToTable("usage_decisions", "yuktira_qm");
    }
}

public class QualityNotificationEntityConfiguration : IEntityTypeConfiguration<QualityNotificationEntity>
{
    public void Configure(EntityTypeBuilder<QualityNotificationEntity> builder)
    {
        builder.ToTable("quality_notifications", "yuktira_qm");
        builder.HasIndex(e => e.NotificationNumber).IsUnique();
    }
}

public class QualityNotificationTaskEntityConfiguration : IEntityTypeConfiguration<QualityNotificationTaskEntity>
{
    public void Configure(EntityTypeBuilder<QualityNotificationTaskEntity> builder)
    {
        builder.ToTable("quality_notification_tasks", "yuktira_qm");
        builder.HasOne<QualityNotificationEntity>()
            .WithMany()
            .HasForeignKey(e => e.NotificationId);
    }
}

public class InspectionResultDetailEntityConfiguration : IEntityTypeConfiguration<InspectionResultDetailEntity>
{
    public void Configure(EntityTypeBuilder<InspectionResultDetailEntity> builder)
    {
        builder.ToTable("inspection_result_details", "yuktira_qm");
    }
}

public class UsageDecisionDetailEntityConfiguration : IEntityTypeConfiguration<UsageDecisionDetailEntity>
{
    public void Configure(EntityTypeBuilder<UsageDecisionDetailEntity> builder)
    {
        builder.ToTable("usage_decision_details", "yuktira_qm");
    }
}
