using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

public class MesRecipe : MesEntityBase
{
    public Guid MaterialId { get; set; }

    [MaxLength(32)]
    public string Version { get; set; } = "1.0";

    public RecipeStatus Status { get; set; } = RecipeStatus.DRAFT;
    public Guid? DocRevId { get; set; }

    [MaxLength(128)]
    public string? ApprovedBy { get; set; }
}

public class MesRecipePhase : MesEntityBase
{
    public Guid RecipeId { get; set; }
    public int PhaseNo { get; set; }

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;
}

public class MesRecipeParam : MesEntityBase
{
    public Guid RecipeId { get; set; }
    public int PhaseNo { get; set; }

    [MaxLength(64)]
    public string ParamCode { get; set; } = string.Empty;

    public double Target { get; set; }
    public double LowLimit { get; set; }
    public double HighLimit { get; set; }

    [MaxLength(32)]
    public string Uom { get; set; } = string.Empty;

    public bool IsCritical { get; set; }
    public Guid? TagId { get; set; }
    public ParamControlMode ControlMode { get; set; } = ParamControlMode.MONITOR;
}

public class MesRecipeDownload : MesEntityBase, IMesAuditAppendOnly
{
    public Guid RecipeId { get; set; }

    [MaxLength(32)]
    public string Version { get; set; } = string.Empty;

    public Guid ResourceId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string Checksum { get; set; } = string.Empty;

    [MaxLength(32)]
    public string AckStatus { get; set; } = "PENDING";

    [MaxLength(128)]
    public string SentBy { get; set; } = string.Empty;

    [MaxLength(64)]
    public string RecordHash { get; set; } = string.Empty;
}

public class MesProcessRun : MesEntityBase
{
    public Guid WoOpId { get; set; }
    public Guid ResourceId { get; set; }
    public Guid RecipeId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    [MaxLength(32)]
    public string State { get; set; } = "RUNNING";

    public Guid? BatchId { get; set; }
    public string ParamSummaryJson { get; set; } = "{}";
}

public class MesAlarm : MesEntityBase
{
    public Guid TagId { get; set; }
    public Guid ResourceId { get; set; }
    public int Severity { get; set; } = 2; // 1 to 4, ISA-18.2
    public AlarmState State { get; set; } = AlarmState.ACTIVE;
    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AckedAt { get; set; }

    [MaxLength(128)]
    public string? AckedBy { get; set; }

    public DateTime? ClearedAt { get; set; }

    [MaxLength(512)]
    public string? Comment { get; set; }
}

public class MesAlarmRule : MesEntityBase
{
    public Guid TagId { get; set; }

    [MaxLength(256)]
    public string Condition { get; set; } = string.Empty;

    public int DebounceSeconds { get; set; } = 5;
    public int Severity { get; set; } = 2;

    [MaxLength(64)]
    public string Action { get; set; } = "ALARM";
}

public class MesInterlock : MesEntityBase
{
    public Guid ResourceId { get; set; }

    [MaxLength(256)]
    public string Condition { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Command { get; set; } = "HOLD";

    [MaxLength(32)]
    public string Status { get; set; } = "ARMED";

    [MaxLength(128)]
    public string? BypassBy { get; set; }

    public DateTime? BypassUntil { get; set; }
}
