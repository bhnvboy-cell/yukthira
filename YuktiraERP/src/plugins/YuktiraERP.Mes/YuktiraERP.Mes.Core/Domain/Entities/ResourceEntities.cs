using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

/// <summary>mes_resource: links to ERP Equipment, WorkCenter, Material, or Employee by ID.</summary>
public class MesResource : MesEntityBase
{
    [MaxLength(64)]
    public string ResourceCode { get; set; } = string.Empty;

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public ResourceType ResourceType { get; set; } = ResourceType.MACHINE;

    [MaxLength(32)]
    public string ErpRefType { get; set; } = "WorkCenter"; // Equipment, WorkCenter, Material, Employee

    public Guid? ErpRefId { get; set; }

    public ResourceState CurrentState { get; set; } = ResourceState.IDLE;

    public DateTime StateSince { get; set; } = DateTime.UtcNow;

    public string CapabilitiesJson { get; set; } = "{}";

    public Guid? ParentResourceId { get; set; } // line, cell
}

/// <summary>mes_resource_state_log (partitioned in PostgreSQL).</summary>
public class MesResourceStateLog : MesEntityBase
{
    public Guid ResourceId { get; set; }
    public ResourceState FromState { get; set; }
    public ResourceState ToState { get; set; }

    [MaxLength(64)]
    public string? ReasonCode { get; set; }

    public StateChangeSource ChangeSource { get; set; } = StateChangeSource.SYSTEM;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public double DurationSeconds { get; set; }
}

/// <summary>mes_state_reason: ISO 22400 aligned loss classification.</summary>
public class MesStateReason : MesEntityBase
{
    [MaxLength(64)]
    public string ReasonCode { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? ParentCode { get; set; }

    [MaxLength(128)]
    public string Description { get; set; } = string.Empty;

    public IsoLossClass LossClass { get; set; } = IsoLossClass.UNPLANNED_DOWN;
}

/// <summary>mes_resource_allocation</summary>
public class MesResourceAllocation : MesEntityBase
{
    public Guid WoOpId { get; set; }
    public Guid ResourceId { get; set; }
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public decimal Qty { get; set; }
    public AllocationStatus Status { get; set; } = AllocationStatus.RESERVED;
}

/// <summary>mes_tool_life</summary>
public class MesToolLife : MesEntityBase
{
    public Guid ToolResourceId { get; set; }
    public long CyclesUsed { get; set; }
    public double RuntimeMin { get; set; }
    public long LifeLimit { get; set; }
    public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
    public int RegrindCount { get; set; }
    public bool BlockAtLimit { get; set; } = false;
}

/// <summary>mes_resource_capability</summary>
public class MesResourceCapability : MesEntityBase
{
    public Guid ResourceId { get; set; }

    [MaxLength(64)]
    public string CapabilityCode { get; set; } = string.Empty;

    public decimal MinRange { get; set; }
    public decimal MaxRange { get; set; }
}
