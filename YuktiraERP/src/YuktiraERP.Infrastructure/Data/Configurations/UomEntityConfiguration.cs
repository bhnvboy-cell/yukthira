using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class UomDimensionEntityConfiguration : IEntityTypeConfiguration<UomDimensionEntity>
{
    public void Configure(EntityTypeBuilder<UomDimensionEntity> builder)
    {
        builder.ToTable("uom_dimensions", "yuktira_core");
        builder.HasIndex(e => new { e.DimensionCode, e.TenantId }).IsUnique().HasDatabaseName("idx_uom_dim_code_tenant");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("idx_uom_dim_tenant");
        builder.Property(e => e.DimensionCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.SiBaseUom).IsRequired().HasMaxLength(10);
        builder.Property(e => e.LongDescription).IsRequired().HasMaxLength(200);
    }
}

public class UnitOfMeasureEntityConfiguration : IEntityTypeConfiguration<UnitOfMeasureEntity>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasureEntity> builder)
    {
        builder.ToTable("units_of_measure", "yuktira_core");
        builder.HasIndex(e => new { e.Msehi, e.TenantId }).IsUnique().HasDatabaseName("idx_uom_msehi_tenant");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("idx_uom_tenant");
        builder.Property(e => e.Msehi).IsRequired().HasMaxLength(10);
        builder.Property(e => e.IsoCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.DimensionCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Numerator).HasColumnType("decimal(18,6)").HasDefaultValue(1m);
        builder.Property(e => e.Denominator).HasColumnType("decimal(18,6)").HasDefaultValue(1m);
        builder.Property(e => e.AddOffset).HasColumnType("decimal(18,6)").HasDefaultValue(0m);
        builder.Property(e => e.ShortText).IsRequired().HasMaxLength(100);
    }
}

public class MaterialUomConversionEntityConfiguration : IEntityTypeConfiguration<MaterialUomConversionEntity>
{
    public void Configure(EntityTypeBuilder<MaterialUomConversionEntity> builder)
    {
        builder.ToTable("material_uom_conversions", "yuktira_core");
        builder.HasIndex(e => new { e.MaterialCode, e.TenantId }).HasDatabaseName("idx_mat_uom_material");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("idx_mat_uom_tenant");
        builder.Property(e => e.MaterialCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.SourceUomCode).IsRequired().HasMaxLength(10);
        builder.Property(e => e.TargetUomCode).IsRequired().HasMaxLength(10);
        builder.Property(e => e.ConversionFactor).HasColumnType("decimal(18,6)");
        builder.Property(e => e.DensityFactor).HasColumnType("decimal(18,6)");
        builder.Property(e => e.PlantCode).HasMaxLength(10);
    }
}
