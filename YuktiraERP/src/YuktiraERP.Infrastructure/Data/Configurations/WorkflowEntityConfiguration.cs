using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Data.Configurations;

public class WorkflowDefinitionEntityConfiguration : IEntityTypeConfiguration<WorkflowDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinitionEntity> builder)
    {
        builder.ToTable("workflow_definitions", "yuktira_workflow");
    }
}

public class WorkflowNodeEntityConfiguration : IEntityTypeConfiguration<WorkflowNodeEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowNodeEntity> builder)
    {
        builder.ToTable("workflow_nodes", "yuktira_workflow");
    }
}

public class WorkflowEdgeEntityConfiguration : IEntityTypeConfiguration<WorkflowEdgeEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowEdgeEntity> builder)
    {
        builder.ToTable("workflow_edges", "yuktira_workflow");
    }
}

public class WorkflowInstanceEntityConfiguration : IEntityTypeConfiguration<WorkflowInstanceEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowInstanceEntity> builder)
    {
        builder.ToTable("workflow_instances", "yuktira_workflow");
    }
}

public class WorkflowHistoryEntityConfiguration : IEntityTypeConfiguration<WorkflowHistoryEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowHistoryEntity> builder)
    {
        builder.ToTable("workflow_history", "yuktira_workflow");
    }
}
