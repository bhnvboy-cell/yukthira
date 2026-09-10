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

public class QMMasterDataEntityConfiguration : IEntityTypeConfiguration<QMMasterDataEntity>
{
    public void Configure(EntityTypeBuilder<QMMasterDataEntity> builder)
    {
        builder.ToTable("qm_master_data", "yuktira_qm");
        builder.HasIndex(e => new { e.MaterialCode, e.Plant }).IsUnique();
    }
}

public class QMInspectionConfigEntityConfiguration : IEntityTypeConfiguration<QMInspectionConfigEntity>
{
    public void Configure(EntityTypeBuilder<QMInspectionConfigEntity> builder)
    {
        builder.ToTable("qm_inspection_configs", "yuktira_qm");
    }
}

public class CertificateOfAnalysisEntityConfiguration : IEntityTypeConfiguration<CertificateOfAnalysisEntity>
{
    public void Configure(EntityTypeBuilder<CertificateOfAnalysisEntity> builder)
    {
        builder.ToTable("certificates_of_analysis", "yuktira_qm");
    }
}

public class StockBalanceEntityConfiguration : IEntityTypeConfiguration<StockBalanceEntity>
{
    public void Configure(EntityTypeBuilder<StockBalanceEntity> builder)
    {
        builder.ToTable("stock_balances", "yuktira_mm");
        builder.Ignore(s => s.Xmin);
        builder.Property(s => s.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(s => s.UnitPrice).HasColumnType("decimal(18,4)");
        builder.Property(s => s.TotalValue).HasColumnType("decimal(18,2)");
    }
}

public class InspectionLotAuditEntityConfiguration : IEntityTypeConfiguration<InspectionLotAuditEntity>
{
    public void Configure(EntityTypeBuilder<InspectionLotAuditEntity> builder)
    {
        builder.ToTable("inspection_lot_audits", "yuktira_qm");
        builder.Property(a => a.StockQuantityMoved).HasColumnType("decimal(18,4)");
        builder.HasOne<InspectionLotEntity>().WithMany().HasForeignKey(a => a.InspectionLotId);
    }
}

public class NonConformanceEntityConfiguration : IEntityTypeConfiguration<NonConformanceEntity>
{
    public void Configure(EntityTypeBuilder<NonConformanceEntity> builder)
    {
        builder.ToTable("non_conformances", "yuktira_qm");
        builder.HasIndex(e => e.NCNumber).IsUnique();
        builder.Property(e => e.AffectedQuantity).HasColumnType("decimal(18,4)");
        builder.Property(e => e.EstimatedCost).HasColumnType("decimal(18,2)");
    }
}

public class CAPAEntityConfiguration : IEntityTypeConfiguration<CAPAEntity>
{
    public void Configure(EntityTypeBuilder<CAPAEntity> builder)
    {
        builder.ToTable("capas", "yuktira_qm");
        builder.HasIndex(e => e.CPNumber).IsUnique();
    }
}

public class HandlingUnitEntityConfiguration : IEntityTypeConfiguration<HandlingUnitEntity>
{
    public void Configure(EntityTypeBuilder<HandlingUnitEntity> builder)
    {
        builder.ToTable("handling_units", "yuktira_qm");
        builder.HasIndex(e => e.HUNumber).IsUnique();
        builder.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
        builder.Property(e => e.GrossWeight).HasColumnType("decimal(18,4)");
        builder.Property(e => e.NetWeight).HasColumnType("decimal(18,4)");
        builder.Property(e => e.Volume).HasColumnType("decimal(18,4)");
    }
}

public class LabAnalysisEntityConfiguration : IEntityTypeConfiguration<LabAnalysisEntity>
{
    public void Configure(EntityTypeBuilder<LabAnalysisEntity> builder)
    {
        builder.ToTable("lab_analyses", "yuktira_qm");
        builder.HasIndex(e => e.AnalysisNumber).IsUnique();
        builder.Property(e => e.RawValue).HasColumnType("decimal(18,6)");
        builder.Property(e => e.ConvertedValue).HasColumnType("decimal(18,6)");
        builder.Property(e => e.LSL).HasColumnType("decimal(18,6)");
        builder.Property(e => e.USL).HasColumnType("decimal(18,6)");
    }
}

public class CoaGenerationLogEntityConfiguration : IEntityTypeConfiguration<CoaGenerationLogEntity>
{
    public void Configure(EntityTypeBuilder<CoaGenerationLogEntity> builder)
    {
        builder.ToTable("coa_generation_logs", "yuktira_qm");
        builder.HasIndex(e => e.CertificateNumber).IsUnique();
    }
}
