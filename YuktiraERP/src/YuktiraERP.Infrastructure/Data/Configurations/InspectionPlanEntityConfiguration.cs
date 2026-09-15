using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class MicMasterEntityConfiguration : IEntityTypeConfiguration<MicMasterEntity>
{
    public void Configure(EntityTypeBuilder<MicMasterEntity> builder)
    {
        builder.ToTable("mic_master", "yuktira_qm");
        builder.HasIndex(e => new { e.CharacteristicCode, e.PlantId }).IsUnique();
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Status);
        builder.Property(e => e.ShortText).HasMaxLength(200);
    }
}

public class QmInspectionPlanHeaderEntityConfiguration : IEntityTypeConfiguration<QmInspectionPlanHeaderEntity>
{
    public void Configure(EntityTypeBuilder<QmInspectionPlanHeaderEntity> builder)
    {
        builder.ToTable("inspection_plan_headers", "yuktira_qm");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.PlantId, e.MaterialId, e.TenantId });
        builder.HasIndex(e => new { e.GroupKey, e.GroupCounter });
        builder.Property(e => e.LotSizeFrom).HasColumnType("decimal(18,6)");
        builder.Property(e => e.LotSizeTo).HasColumnType("decimal(18,6)");
    }
}

public class QmInspectionPlanOperationEntityConfiguration : IEntityTypeConfiguration<QmInspectionPlanOperationEntity>
{
    public void Configure(EntityTypeBuilder<QmInspectionPlanOperationEntity> builder)
    {
        builder.ToTable("inspection_plan_operations", "yuktira_qm");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.PlanHeaderId);
        builder.HasOne<QmInspectionPlanHeaderEntity>()
            .WithMany(h => h.Operations)
            .HasForeignKey(e => e.PlanHeaderId);
        builder.Property(e => e.BaseQuantity).HasColumnType("decimal(18,6)");
    }
}

public class QmInspectionPlanMicEntityConfiguration : IEntityTypeConfiguration<QmInspectionPlanMicEntity>
{
    public void Configure(EntityTypeBuilder<QmInspectionPlanMicEntity> builder)
    {
        builder.ToTable("inspection_plan_characteristics", "yuktira_qm");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.PlanHeaderId);
        builder.HasIndex(e => e.OperationId);
        builder.HasIndex(e => new { e.PlanHeaderId, e.OperationId, e.CharacteristicNo });
        builder.HasOne<QmInspectionPlanHeaderEntity>()
            .WithMany()
            .HasForeignKey(e => e.PlanHeaderId);
        builder.HasOne<QmInspectionPlanOperationEntity>()
            .WithMany(o => o.Characteristics)
            .HasForeignKey(e => e.OperationId);
    }
}
