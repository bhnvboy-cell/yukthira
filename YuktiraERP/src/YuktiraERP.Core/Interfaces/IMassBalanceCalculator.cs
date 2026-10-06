namespace YuktiraERP.Core.Interfaces;

public class MassBalanceOptions
{
    public const string SectionName = "MassBalance";

    public decimal DefaultThresholdPct { get; set; } = 2.0m;
}

public static class MassBalanceKinds
{
    public const string RawInput = "RawInput";
    public const string Intermediate = "Intermediate";
    public const string PrimaryOutput = "PrimaryOutput";
    public const string CoProduct = "CoProduct";
}

public class MassBalanceStream
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = MassBalanceKinds.RawInput;
    public decimal MassKg { get; set; }
    public decimal MoisturePct { get; set; }
    public decimal WaterKg { get; set; }
}

public class MassBalanceInput
{
    public List<MassBalanceStream>? Streams { get; set; }
    public decimal? ThresholdPct { get; set; }
}

public class MassBalanceResult
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = "";
    public decimal TotalInputKg { get; set; }
    public decimal TotalOutputKg { get; set; }
    public decimal TotalCoProductKg { get; set; }
    public decimal YieldLossKg { get; set; }
    public decimal YieldLossPct { get; set; }
    public decimal PhysicalYieldPct { get; set; }
    public decimal DrySubstanceInputKg { get; set; }
    public decimal DrySubstanceOutputKg { get; set; }
    public decimal DrySubstanceLossKg { get; set; }
    public decimal DrySubstanceYieldPct { get; set; }
    public decimal ThresholdPct { get; set; }
    public string Status { get; set; } = "WithinTolerance";
    public string BreakdownJson { get; set; } = "{}";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public interface IMassBalanceCalculator
{
    MassBalanceResult Calculate(MassBalanceInput input);

    Task<MassBalanceResult> CalculateForOrderAsync(Guid productionOrderId, string userId, CancellationToken ct);

    Task<MassBalanceResult> CalculateAdHocAsync(MassBalanceInput input, string userId, CancellationToken ct);
}
