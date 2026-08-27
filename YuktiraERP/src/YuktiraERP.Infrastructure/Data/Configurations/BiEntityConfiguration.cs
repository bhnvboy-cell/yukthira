using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class BIReportEntityConfiguration : IEntityTypeConfiguration<BIReportEntity>
{
    public void Configure(EntityTypeBuilder<BIReportEntity> builder)
    {
        builder.ToTable("bi_reports", "yuktira_bi");
    }
}

public class DashboardEntityConfiguration : IEntityTypeConfiguration<DashboardEntity>
{
    public void Configure(EntityTypeBuilder<DashboardEntity> builder)
    {
        builder.ToTable("dashboard_widgets", "yuktira_dashboard");
    }
}

public class KpiSnapshotEntityConfiguration : IEntityTypeConfiguration<KpiSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<KpiSnapshotEntity> builder)
    {
        builder.ToTable("bi_kpis", "yuktira_bi");
        builder.Property(e => e.Value).HasColumnType("decimal(18,2)");
    }
}
