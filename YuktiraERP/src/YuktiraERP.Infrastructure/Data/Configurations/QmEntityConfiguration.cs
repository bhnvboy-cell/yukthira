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
