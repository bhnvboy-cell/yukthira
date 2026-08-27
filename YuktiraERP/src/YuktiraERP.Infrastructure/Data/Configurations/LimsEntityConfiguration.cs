using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class SampleEntityConfiguration : IEntityTypeConfiguration<SampleEntity>
{
    public void Configure(EntityTypeBuilder<SampleEntity> builder)
    {
        builder.ToTable("lab_samples", "yuktira_lims");
    }
}

public class TestResultEntityConfiguration : IEntityTypeConfiguration<TestResultEntity>
{
    public void Configure(EntityTypeBuilder<TestResultEntity> builder)
    {
        builder.ToTable("lab_sample_tests", "yuktira_lims");
    }
}

public class SpecificationEntityConfiguration : IEntityTypeConfiguration<SpecificationEntity>
{
    public void Configure(EntityTypeBuilder<SpecificationEntity> builder)
    {
        builder.ToTable("lab_specifications", "yuktira_lims");
    }
}

public class InstrumentEntityConfiguration : IEntityTypeConfiguration<InstrumentEntity>
{
    public void Configure(EntityTypeBuilder<InstrumentEntity> builder)
    {
        builder.ToTable("lab_instruments", "yuktira_lims");
    }
}
