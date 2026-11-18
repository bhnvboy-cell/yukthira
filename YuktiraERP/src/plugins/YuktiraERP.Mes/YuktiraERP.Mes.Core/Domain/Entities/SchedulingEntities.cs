using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

public class MesScheduleVersion : MesEntityBase
{
    [MaxLength(64)]
    public string VersionCode { get; set; } = string.Empty;

    public DateTime HorizonStart { get; set; }
    public DateTime HorizonEnd { get; set; }
    public ScheduleVersionStatus Status { get; set; } = ScheduleVersionStatus.DRAFT;

    [MaxLength(64)]
    public string Algorithm { get; set; } = "EDD_CPSAT";

    public string KpisJson { get; set; } = "{}";
}

public class MesScheduleOp : MesEntityBase
{
    public Guid VersionId { get; set; }
    public Guid ProdOrderId { get; set; }
    public int OperationNo { get; set; }
    public Guid ResourceId { get; set; }
    public DateTime SetupStart { get; set; }
    public DateTime SetupEnd { get; set; }
    public DateTime RunStart { get; set; }
    public DateTime RunEnd { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsLocked { get; set; }
    public Guid? PredecessorOpId { get; set; }
    public double SlackMin { get; set; }
}

public class MesChangeoverMatrix : MesEntityBase
{
    [MaxLength(64)]
    public string ResourceGroup { get; set; } = string.Empty;

    [MaxLength(64)]
    public string FromFamily { get; set; } = string.Empty;

    [MaxLength(64)]
    public string ToFamily { get; set; } = string.Empty;

    public double SetupMin { get; set; }
    public decimal SetupCost { get; set; }
    public bool NeedsCleaning { get; set; }
}

public class MesShiftCalendar : MesEntityBase
{
    public Guid? ResourceId { get; set; }

    [MaxLength(128)]
    public string ShiftPattern { get; set; } = "3x8h";

    public string ExceptionsJson { get; set; } = "[]";
}

public class MesScheduleConflict : MesEntityBase
{
    public Guid SchedOpId { get; set; }
    public ScheduleConflictType ConflictType { get; set; }

    [MaxLength(512)]
    public string Detail { get; set; } = string.Empty;

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

public class MesPriorityRule : MesEntityBase
{
    [MaxLength(64)]
    public string RuleCode { get; set; } = string.Empty;

    public string WeightsJson { get; set; } = "{}";
}
