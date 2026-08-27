using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("audit_log", "yuktira_audit");
    }
}

public class NotificationEntityConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("notifications", "yuktira_notification");
    }
}
