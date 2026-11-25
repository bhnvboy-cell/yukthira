using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Domain.Security;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class AuditChangeLogConfiguration : IEntityTypeConfiguration<AuditChangeLog>
{
    public void Configure(EntityTypeBuilder<AuditChangeLog> builder)
    {
        builder.ToTable("change_logs", "audit");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TimestampUtc);
        builder.HasIndex(e => new { e.EntityType, e.EntityPk });
        builder.HasIndex(e => e.ActorUserId);
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EntityPk).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(10).IsRequired();
        builder.Property(e => e.IpAddress).HasMaxLength(50);
        builder.Property(e => e.ChangedFieldsJson).HasColumnType("jsonb");
    }
}

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("messages", "outbox");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.IsPublished, e.RetryCount, e.OccurredAtUtc });
        builder.HasIndex(e => e.AggregateId);
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.EventType).HasMaxLength(250).IsRequired();
        builder.Property(e => e.AggregateId).HasMaxLength(100);
        builder.Property(e => e.PayloadJson).HasColumnType("jsonb");
    }
}

public class IdentityUserConfiguration : IEntityTypeConfiguration<IdentityUser>
{
    public void Configure(EntityTypeBuilder<IdentityUser> builder)
    {
        builder.ToTable("users", "identity");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.Username).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150).IsRequired();
        builder.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
    }
}

public class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.ToTable("roles", "identity");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.Code, e.TenantId }).IsUnique();

        builder.Property(e => e.Code).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(300);
    }
}

public class IdentityPermissionConfiguration : IEntityTypeConfiguration<IdentityPermission>
{
    public void Configure(EntityTypeBuilder<IdentityPermission> builder)
    {
        builder.ToTable("permissions", "identity");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Code).IsUnique();

        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Module).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Resource).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(250);
    }
}

public class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole> builder)
    {
        builder.ToTable("user_roles", "identity");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.RoleId, e.TenantId }).IsUnique();
    }
}

public class IdentityRolePermissionConfiguration : IEntityTypeConfiguration<IdentityRolePermission>
{
    public void Configure(EntityTypeBuilder<IdentityRolePermission> builder)
    {
        builder.ToTable("role_permissions", "identity");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
    }
}

public class SodRuleConfiguration : IEntityTypeConfiguration<SodRuleEntity>
{
    public void Configure(EntityTypeBuilder<SodRuleEntity> builder)
    {
        builder.ToTable("sod_rules", "identity");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.RuleCode, e.TenantId }).IsUnique();

        builder.Property(e => e.RuleCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.RuleName).HasMaxLength(150).IsRequired();
        builder.Property(e => e.FirstRoleOrPermission).HasMaxLength(100).IsRequired();
        builder.Property(e => e.SecondRoleOrPermission).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
    }
}

public class AuthorizationRuleConfiguration : IEntityTypeConfiguration<AuthorizationRule>
{
    public void Configure(EntityTypeBuilder<AuthorizationRule> builder)
    {
        builder.ToTable("authorization_rules", "identity");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.DocumentType, e.TenantId });

        builder.Property(e => e.RuleId).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DocumentType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.RequiredRole).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(300);
        builder.Property(e => e.MinThreshold).HasPrecision(18, 4);
        builder.Property(e => e.MaxThreshold).HasPrecision(18, 4);
    }
}
