using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class NextGenEntityConfiguration
{
    public class SxAuditConfiguration : IEntityTypeConfiguration<SxAuditEntity>
    {
        public void Configure(EntityTypeBuilder<SxAuditEntity> builder)
        {
            builder.ToTable("sx_audit", "yuktira_sx");
            builder.Property(e => e.ActionCategory).HasMaxLength(64);
            builder.Property(e => e.TargetEntity).HasMaxLength(128);
            builder.Property(e => e.TargetId).HasMaxLength(128);
            builder.Property(e => e.Status).HasMaxLength(32);
            builder.Property(e => e.UserId).HasMaxLength(128);
            builder.Property(e => e.PreviousHash).HasMaxLength(80);
            builder.Property(e => e.CurrentHash).HasMaxLength(80);
            builder.HasIndex(e => e.TenantId);
            builder.HasIndex(e => e.SequenceNumber);
        }
    }

    public class FinancialEventConfiguration : IEntityTypeConfiguration<FinancialEventEntity>
    {
        public void Configure(EntityTypeBuilder<FinancialEventEntity> builder)
        {
            builder.ToTable("financial_events", "yuktira_fi");
            builder.Property(e => e.StreamType).HasMaxLength(64);
            builder.Property(e => e.EventType).HasMaxLength(64);
            builder.Property(e => e.CorrelationId).HasMaxLength(128);
            builder.Property(e => e.Status).HasMaxLength(32);
            builder.Property(e => e.PreviousHash).HasMaxLength(80);
            builder.Property(e => e.Hash).HasMaxLength(80);
            builder.HasIndex(e => e.TenantId);
            builder.HasIndex(e => new { e.StreamId, e.Sequence });
            builder.HasIndex(e => e.Status);
        }
    }

    public class EmissionFactorConfiguration : IEntityTypeConfiguration<EmissionFactorEntity>
    {
        public void Configure(EntityTypeBuilder<EmissionFactorEntity> builder)
        {
            builder.ToTable("emission_factors", "yuktira_esg");
            builder.Property(e => e.SourceType).HasMaxLength(64);
            builder.Property(e => e.MaterialCode).HasMaxLength(64);
            builder.Property(e => e.Unit).HasMaxLength(32);
            builder.Property(e => e.Region).HasMaxLength(64);
            builder.HasIndex(e => e.TenantId);
            builder.HasIndex(e => new { e.Scope, e.SourceType, e.IsActive });
        }
    }

    public class EmissionLogConfiguration : IEntityTypeConfiguration<EmissionLogEntity>
    {
        public void Configure(EntityTypeBuilder<EmissionLogEntity> builder)
        {
            builder.ToTable("emission_logs", "yuktira_esg");
            builder.Property(e => e.SourceType).HasMaxLength(64);
            builder.Property(e => e.ReferenceType).HasMaxLength(64);
            builder.Property(e => e.ReferenceId).HasMaxLength(128);
            builder.Property(e => e.MaterialCode).HasMaxLength(64);
            builder.Property(e => e.Unit).HasMaxLength(32);
            builder.Property(e => e.Period).HasMaxLength(16);
            builder.HasIndex(e => e.TenantId);
            builder.HasIndex(e => new { e.Period, e.Scope });
        }
    }

    public class MassBalanceResultConfiguration : IEntityTypeConfiguration<MassBalanceResultEntity>
    {
        public void Configure(EntityTypeBuilder<MassBalanceResultEntity> builder)
        {
            builder.ToTable("mass_balance_results", "yuktira_pp");
            builder.Property(e => e.OrderNumber).HasMaxLength(64);
            builder.Property(e => e.Status).HasMaxLength(32);
            builder.HasIndex(e => e.TenantId);
            builder.HasIndex(e => e.ProductionOrderId);
        }
    }
}
