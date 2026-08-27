using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class LeadEntityConfiguration : IEntityTypeConfiguration<LeadEntity>
{
    public void Configure(EntityTypeBuilder<LeadEntity> builder)
    {
        builder.ToTable("crm_leads", "yuktira_crm");
        builder.Property(e => e.Value).HasColumnType("decimal(18,2)");
    }
}

public class OpportunityEntityConfiguration : IEntityTypeConfiguration<OpportunityEntity>
{
    public void Configure(EntityTypeBuilder<OpportunityEntity> builder)
    {
        builder.ToTable("crm_opportunities", "yuktira_crm");
        builder.Property(e => e.Value).HasColumnType("decimal(18,2)");
    }
}

public class ContactEntityConfiguration : IEntityTypeConfiguration<ContactEntity>
{
    public void Configure(EntityTypeBuilder<ContactEntity> builder)
    {
        builder.ToTable("crm_contacts", "yuktira_crm");
    }
}

public class CampaignEntityConfiguration : IEntityTypeConfiguration<CampaignEntity>
{
    public void Configure(EntityTypeBuilder<CampaignEntity> builder)
    {
        builder.ToTable("crm_campaigns", "yuktira_crm");
        builder.Property(e => e.Budget).HasColumnType("decimal(18,2)");
    }
}

public class ServiceTicketEntityConfiguration : IEntityTypeConfiguration<ServiceTicketEntity>
{
    public void Configure(EntityTypeBuilder<ServiceTicketEntity> builder)
    {
        builder.ToTable("crm_service_tickets", "yuktira_crm");
    }
}
