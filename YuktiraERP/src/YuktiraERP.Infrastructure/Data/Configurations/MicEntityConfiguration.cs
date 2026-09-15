using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class MicEntityConfiguration : IEntityTypeConfiguration<MicMasterEntity>
{
    public void Configure(EntityTypeBuilder<MicMasterEntity> builder)
    {
        builder.ToTable("mic_master", "yuktira_qm");

        builder.HasIndex(e => new { e.PlantId, e.CharacteristicCode, e.TenantId })
            .IsUnique()
            .HasDatabaseName("idx_mic_composite");

        builder.HasIndex(e => new { e.Status, e.TenantId })
            .HasDatabaseName("idx_mic_status");

        builder.HasIndex(e => new { e.PlantId, e.TenantId })
            .HasDatabaseName("idx_mic_plant");

        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("idx_mic_tenant");

        builder.Property(e => e.LowerTolerance)
            .HasColumnType("decimal(18,6)");

        builder.Property(e => e.UpperTolerance)
            .HasColumnType("decimal(18,6)");

        builder.Property(e => e.TargetValue)
            .HasColumnType("decimal(18,6)");
    }
}
