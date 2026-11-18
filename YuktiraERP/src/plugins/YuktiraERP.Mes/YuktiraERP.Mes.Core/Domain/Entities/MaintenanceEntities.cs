using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

public class MesEquipmentMeter : MesEntityBase
{
    public Guid EquipmentId { get; set; } // FK to PM Equipment
    public MeterType MeterType { get; set; } = MeterType.HOURS;
    public double Counter { get; set; }
    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;
    public double LastPmCounter { get; set; }
}

public class MesPmTrigger : MesEntityBase
{
    public Guid EquipmentId { get; set; }
    public PmTriggerType TriggerType { get; set; } = PmTriggerType.METER;
    public double Threshold { get; set; }
    public DateTime? NextDue { get; set; }
    public Guid? PlanId { get; set; } // FK to PM MaintenancePlan
}

public class MesDowntimeEvent : MesEntityBase
{
    public Guid ResourceId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    [MaxLength(64)]
    public string? ReasonCode { get; set; }

    [MaxLength(64)]
    public string? FailureCode { get; set; }

    public Guid? NotificationId { get; set; }
    public Guid? MaintOrderId { get; set; }
    public double ResponseMin { get; set; }
    public double RepairMin { get; set; }
}

public class MesAndonCall : MesEntityBase
{
    public Guid LineId { get; set; }
    public AndonCallType CallType { get; set; } = AndonCallType.MAINTENANCE;
    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AckedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    [MaxLength(128)]
    public string RaisedBy { get; set; } = string.Empty;
}

public class MesConditionReading : MesEntityBase
{
    public Guid EquipmentId { get; set; }
    public string FeaturesJson { get; set; } = "{}";
    public double AnomalyScore { get; set; }

    [MaxLength(32)]
    public string ModelVersion { get; set; } = "1.0";
}

public class MesFailureCode : MesEntityBase
{
    [MaxLength(64)]
    public string ProblemCode { get; set; } = string.Empty;

    [MaxLength(64)]
    public string CauseCode { get; set; } = string.Empty;

    [MaxLength(64)]
    public string ActionCode { get; set; } = string.Empty;
}

public class MesLoto : MesEntityBase
{
    public Guid EquipmentId { get; set; }
    public string LocksJson { get; set; } = "[]";

    [MaxLength(128)]
    public string IssuedBy { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? ClearedBy { get; set; }

    public DateTime? ClearedAt { get; set; }
}
