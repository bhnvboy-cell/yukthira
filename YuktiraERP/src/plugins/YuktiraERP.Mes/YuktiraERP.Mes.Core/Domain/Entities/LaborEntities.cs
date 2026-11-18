using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

public class MesLaborClock : MesEntityBase
{
    public Guid EmployeeId { get; set; }
    public DateTime ClockIn { get; set; } = DateTime.UtcNow;
    public DateTime? ClockOut { get; set; }

    [MaxLength(64)]
    public string TerminalId { get; set; } = string.Empty;

    public Guid? ShiftId { get; set; }
    public LaborClockStatus Status { get; set; } = LaborClockStatus.OPEN;
}

public class MesLaborTaskLog : MesEntityBase
{
    public Guid EmployeeId { get; set; }
    public Guid? WoOpId { get; set; }
    public Guid? ResourceId { get; set; }
    public DateTime StartTs { get; set; } = DateTime.UtcNow;
    public DateTime? EndTs { get; set; }
    public LaborActivityType ActivityType { get; set; } = LaborActivityType.DIRECT;

    [MaxLength(64)]
    public string? ReasonCode { get; set; }
}

public class MesSkill : MesEntityBase
{
    [MaxLength(64)]
    public string SkillCode { get; set; } = string.Empty;

    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Category { get; set; } = "OPERATOR";
}

public class MesEmployeeSkill : MesEntityBase
{
    public Guid EmployeeId { get; set; }
    public Guid SkillId { get; set; }
    public int Level { get; set; } = 1; // 1 to 5
    public DateTime CertifiedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresOn { get; set; }
    public Guid? EvidenceDocId { get; set; }
}

public class MesOperationSkillReq : MesEntityBase
{
    public Guid? RoutingOpId { get; set; }
    public Guid? ResourceId { get; set; }
    public Guid SkillId { get; set; }
    public int MinLevel { get; set; } = 1;
}

public class MesShiftAssignment : MesEntityBase
{
    public Guid EmployeeId { get; set; }
    public Guid ShiftId { get; set; }
    public Guid WorkCenterId { get; set; }
    public DateOnly Date { get; set; }
    public decimal PlannedHours { get; set; } = 8m;
}

public class MesLaborException : MesEntityBase
{
    public Guid EmployeeId { get; set; }
    public LaborExceptionType ExceptionType { get; set; }

    [MaxLength(32)]
    public string Status { get; set; } = "OPEN";
}
