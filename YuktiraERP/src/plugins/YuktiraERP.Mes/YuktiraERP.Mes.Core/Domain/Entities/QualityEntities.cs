using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

public class MesInspectionTask : MesEntityBase
{
    public Guid? InspectionLotId { get; set; } // FK to QM InspectionLot
    public Guid WoOpId { get; set; }
    public Guid? PlanId { get; set; }
    public InspectionTriggerType TriggerType { get; set; } = InspectionTriggerType.FIRST_OFF;
    public DateTime DueAt { get; set; } = DateTime.UtcNow;

    [MaxLength(32)]
    public string Status { get; set; } = "OPEN";
}

public class MesQcResult : MesEntityBase
{
    public Guid TaskId { get; set; }
    public Guid CharacteristicId { get; set; }
    public int SampleNo { get; set; }
    public double Value { get; set; }

    [MaxLength(32)]
    public string Uom { get; set; } = string.Empty;

    public bool InTolerance { get; set; }
    public Guid? DeviceId { get; set; }
    public Guid? UnitId { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

public class MesSpcChart : MesEntityBase
{
    public Guid CharacteristicId { get; set; }
    public Guid ResourceId { get; set; }
    public SpcChartType ChartType { get; set; } = SpcChartType.XBAR_R;
    public int SubgroupSize { get; set; } = 5;
    public double Ucl { get; set; }
    public double Lcl { get; set; }
    public double Cl { get; set; }
    public int LimitsVersion { get; set; } = 1;

    [MaxLength(64)]
    public string RuleSet { get; set; } = "WESTERN_ELECTRIC";
}

public class MesSpcPoint : MesEntityBase
{
    public Guid ChartId { get; set; }
    public double Mean { get; set; }
    public double Range { get; set; }
    public double StdDev { get; set; }
    public int SampleCount { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public class MesSpcViolation : MesEntityBase
{
    public Guid ChartId { get; set; }
    public Guid PointId { get; set; }

    [MaxLength(64)]
    public string RuleId { get; set; } = string.Empty; // e.g., WE_RULE_1

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(128)]
    public string ActionTaken { get; set; } = "ALERT";
}

public class MesNonConformance : MesEntityBase
{
    [MaxLength(64)]
    public string NcCode { get; set; } = string.Empty;

    [MaxLength(64)]
    public string DefectCode { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Severity { get; set; } = "MAJOR";

    public decimal Qty { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? LotId { get; set; }
    public NcDisposition Disposition { get; set; } = NcDisposition.REWORK;
    public Guid? CapaId { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "OPEN";
}

/// <summary>mes_hold: append-only audit record hash on placement/release.</summary>
public class MesHold : MesEntityBase, IMesAuditAppendOnly
{
    public HoldTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    [MaxLength(256)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(128)]
    public string PlacedBy { get; set; } = string.Empty;

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(128)]
    public string? ReleasedBy { get; set; }

    public DateTime? ReleasedAt { get; set; }
    public HoldStatus Status { get; set; } = HoldStatus.ACTIVE;

    [MaxLength(64)]
    public string RecordHash { get; set; } = string.Empty;
}

public class MesCapabilitySnapshot : MesEntityBase
{
    public Guid CharacteristicId { get; set; }

    [MaxLength(32)]
    public string Period { get; set; } = string.Empty;

    public double Cp { get; set; }
    public double Cpk { get; set; }
    public double Pp { get; set; }
    public double Ppk { get; set; }
    public int SampleN { get; set; }
}
