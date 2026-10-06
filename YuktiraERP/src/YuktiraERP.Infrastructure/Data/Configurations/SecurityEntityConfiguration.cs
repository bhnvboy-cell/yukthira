using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class MasterRoleEntityConfiguration : IEntityTypeConfiguration<MasterRoleEntity>
{
    public void Configure(EntityTypeBuilder<MasterRoleEntity> builder)
    {
        builder.ToTable("master_roles", "yuktira_sys");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.RoleId, e.TenantId }).IsUnique();
        builder.HasIndex(e => e.Module);
        builder.Property(e => e.RoleId).HasMaxLength(50);
        builder.Property(e => e.RoleName).HasMaxLength(200);
        builder.Property(e => e.Module).HasMaxLength(50);
        builder.Property(e => e.SubProcess).HasMaxLength(100);
        builder.Property(e => e.Catalog).HasMaxLength(100);
        builder.Property(e => e.Space).HasMaxLength(100);
        builder.Property(e => e.Status).HasMaxLength(20);
    }
}

public class CompositeRoleEntityConfiguration : IEntityTypeConfiguration<CompositeRoleEntity>
{
    public void Configure(EntityTypeBuilder<CompositeRoleEntity> builder)
    {
        builder.ToTable("composite_roles", "yuktira_sys");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.CompositeRoleId, e.TenantId }).IsUnique();
        builder.HasIndex(e => e.Module);
        builder.Property(e => e.CompositeRoleId).HasMaxLength(50);
        builder.Property(e => e.CompositeRoleName).HasMaxLength(200);
        builder.Property(e => e.Module).HasMaxLength(50);
        builder.Property(e => e.Status).HasMaxLength(20);
    }
}

public class DerivedRoleEntityConfiguration : IEntityTypeConfiguration<DerivedRoleEntity>
{
    public void Configure(EntityTypeBuilder<DerivedRoleEntity> builder)
    {
        builder.ToTable("derived_roles", "yuktira_sys");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.DerivedRoleId, e.TenantId }).IsUnique();
        builder.HasIndex(e => e.CompositeRoleId);
        builder.HasIndex(e => e.MasterRoleId);
        builder.Property(e => e.DerivedRoleId).HasMaxLength(50);
        builder.Property(e => e.DerivedRoleName).HasMaxLength(200);
        builder.Property(e => e.CompositeRoleId).HasMaxLength(50);
        builder.Property(e => e.MasterRoleId).HasMaxLength(50);
        builder.Property(e => e.Module).HasMaxLength(50);
        builder.Property(e => e.Status).HasMaxLength(20);
    }
}

public class RoleTCodeAssignmentEntityConfiguration : IEntityTypeConfiguration<RoleTCodeAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<RoleTCodeAssignmentEntity> builder)
    {
        builder.ToTable("role_tcode_assignments", "yuktira_sys");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.RoleId, e.RoleType, e.TenantId });
        builder.HasIndex(e => new { e.TransactionCode, e.TenantId });
        builder.Property(e => e.RoleId).HasMaxLength(50);
        builder.Property(e => e.RoleType).HasMaxLength(20);
        builder.Property(e => e.TransactionCode).HasMaxLength(50);
        builder.Property(e => e.AppId).HasMaxLength(50);
        builder.Property(e => e.AppDescription).HasMaxLength(500);
    }
}

public class SecurityImportBatchEntityConfiguration : IEntityTypeConfiguration<SecurityImportBatchEntity>
{
    public void Configure(EntityTypeBuilder<SecurityImportBatchEntity> builder)
    {
        builder.ToTable("security_import_batches", "yuktira_sys");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.BatchNumber, e.TenantId }).IsUnique();
        builder.Property(e => e.BatchNumber).HasMaxLength(50);
        builder.Property(e => e.FileName).HasMaxLength(200);
        builder.Property(e => e.Status).HasMaxLength(20);
        builder.Property(e => e.ImportedBy).HasMaxLength(50);
    }
}

public class UserRoleAssignmentEntityConfiguration : IEntityTypeConfiguration<UserRoleAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignmentEntity> builder)
    {
        builder.ToTable("user_role_assignments", "yuktira_sys");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.UserId, e.CompositeRoleId, e.TenantId }).IsUnique();
        builder.HasIndex(e => e.CompositeRoleId);
        builder.Property(e => e.UserId).HasMaxLength(50);
        builder.Property(e => e.CompositeRoleId).HasMaxLength(50);
        builder.Property(e => e.AssignedBy).HasMaxLength(50);
        builder.Property(e => e.Status).HasMaxLength(20);
    }
}

public class AuthorizationTraceEntityConfiguration : IEntityTypeConfiguration<AuthorizationTraceEntity>
{
    public void Configure(EntityTypeBuilder<AuthorizationTraceEntity> builder)
    {
        builder.ToTable("authorization_traces", "yuktira_security");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.CreatedAt });
        builder.HasIndex(e => new { e.TenantId, e.Decision });
        builder.Property(e => e.UserId);
        builder.Property(e => e.UserName).HasMaxLength(200);
        builder.Property(e => e.Role).HasMaxLength(50);
        builder.Property(e => e.SessionId).HasMaxLength(100);
        builder.Property(e => e.CorrelationId).HasMaxLength(100);
        builder.Property(e => e.ResourceType).HasMaxLength(20);
        builder.Property(e => e.Resource).HasMaxLength(300);
        builder.Property(e => e.Decision).HasMaxLength(10);
        builder.Property(e => e.RuleSource).HasMaxLength(50);
        builder.Property(e => e.Reason).HasMaxLength(1000);
        builder.Property(e => e.HttpMethod).HasMaxLength(10);
        builder.Property(e => e.HttpPath).HasMaxLength(500);
        builder.Property(e => e.IpAddress).HasMaxLength(60);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
    }
}

public class TCodeAuthCheckEntityConfiguration : IEntityTypeConfiguration<TCodeAuthCheckEntity>
{
    public void Configure(EntityTypeBuilder<TCodeAuthCheckEntity> builder)
    {
        builder.ToTable("tcode_auth_checks", "yuktira_security");
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TCode, e.TenantId });
        builder.Property(e => e.TCode).HasMaxLength(50);
        builder.Property(e => e.CheckCode).HasMaxLength(50);
        builder.Property(e => e.ActionType).HasMaxLength(20);
        builder.Property(e => e.RequiredRole).HasMaxLength(50);
        builder.Property(e => e.Enforcement).HasMaxLength(20);
        builder.Property(e => e.Description).HasMaxLength(500);
    }
}
