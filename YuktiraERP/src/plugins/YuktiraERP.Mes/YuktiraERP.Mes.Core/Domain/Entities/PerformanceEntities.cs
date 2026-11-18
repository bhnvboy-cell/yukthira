using System.ComponentModel.DataAnnotations;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Enums;

namespace YuktiraERP.Mes.Core.Domain.Entities;

public class MesKpiDefinition : MesEntityBase
{
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Formula { get; set; } = string.Empty;

    public KpiScope Scope { get; set; } = KpiScope.RESOURCE;
    public double Target { get; set; }
    public double WarnThreshold { get; set; }

    [MaxLength(128)]
    public string Owner { get; set; } = string.Empty;
}

public class MesOeeInterval : MesEntityBase
{
    public Guid ResourceId { get; set; }
    public DateTime IntervalStart { get; set; }
    public OeeIntervalType IntervalType { get; set; } = OeeIntervalType.MINUTE;
    public double PlannedTimeS { get; set; }
    public double RunTimeS { get; set; }
    public double IdealCycleS { get; set; }
    public long TotalCount { get; set; }
    public long GoodCount { get; set; }
    public double Availability { get; set; }
    public double Performance { get; set; }
    public double Quality { get; set; }
    public double Oee { get; set; }
    public bool IsDataGapped { get; set; }
}

public class MesLossEvent : MesEntityBase
{
    public Guid ResourceId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    [MaxLength(64)]
    public string LossCategory { get; set; } = "BREAKDOWN"; // Six Big Losses

    [MaxLength(64)]
    public string? ReasonCode { get; set; }

    public double DurationS { get; set; }
    public Guid? WoOpId { get; set; }
}

public class MesCycleTimeStat : MesEntityBase
{
    public Guid ResourceId { get; set; }
    public Guid MaterialId { get; set; }
    public int OpNo { get; set; }
    public double P50 { get; set; }
    public double P90 { get; set; }
    public double StdDev { get; set; }
    public double Standard { get; set; }
    public int SampleN { get; set; }
}

public class MesScheduleAdherence : MesEntityBase
{
    public Guid WoOpId { get; set; }
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public double StartDeltaMin { get; set; }
    public double EndDeltaMin { get; set; }
    public bool OnTimeFlag { get; set; }
}

public class MesKpiSnapshot : MesEntityBase
{
    [MaxLength(64)]
    public string ScopeKey { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }
    public double Fpy { get; set; }
    public double Otd { get; set; }
    public double WipAgeHours { get; set; }
    public double ScrapRate { get; set; }
    public double MtbfHours { get; set; }
    public double MttrHours { get; set; }
    public double LaborEfficiency { get; set; }
    public double EnergyPerUnit { get; set; }
}
