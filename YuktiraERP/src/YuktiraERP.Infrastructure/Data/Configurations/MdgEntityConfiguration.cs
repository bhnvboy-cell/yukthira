using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class MdgChangeRequestEntityConfiguration : IEntityTypeConfiguration<MdgChangeRequestEntity>
{
    public void Configure(EntityTypeBuilder<MdgChangeRequestEntity> builder)
    {
        builder.ToTable("change_requests", "yuktira_mdg");
        builder.Property(e => e.RequestNumber).HasMaxLength(30);
        builder.Property(e => e.EntityName).HasMaxLength(100);
        builder.Property(e => e.EntityId).HasMaxLength(100);
        builder.Property(e => e.StagingPayload).HasColumnType("jsonb");
        builder.Property(e => e.Status).HasMaxLength(20);
        builder.Property(e => e.RequestedBy).HasMaxLength(100);
        builder.Property(e => e.ApprovedBy).HasMaxLength(100);
        builder.Property(e => e.RejectionReason).HasColumnType("text");
        builder.HasIndex(e => e.TenantId).HasDatabaseName("idx_mdg_tenant");
        builder.HasIndex(e => e.Status).HasDatabaseName("idx_mdg_status");
        builder.HasIndex(e => new { e.RequestNumber, e.TenantId }).IsUnique().HasDatabaseName("idx_mdg_request_number");
    }
}

public class MdgAuditLogEntityConfiguration : IEntityTypeConfiguration<MdgAuditLogEntity>
{
    public void Configure(EntityTypeBuilder<MdgAuditLogEntity> builder)
    {
        builder.ToTable("audit_logs", "yuktira_mdg");
        builder.Property(e => e.Action).HasMaxLength(50);
        builder.Property(e => e.Actor).HasMaxLength(100);
        builder.Property(e => e.HashSha256).HasMaxLength(64);
        builder.HasIndex(e => e.TenantId).HasDatabaseName("idx_mdg_audit_tenant");
        builder.HasIndex(e => e.ChangeRequestId).HasDatabaseName("idx_mdg_audit_cr");
    }
}
