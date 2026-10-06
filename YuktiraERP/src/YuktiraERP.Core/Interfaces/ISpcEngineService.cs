using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

/// <summary>A single out-of-spec / in-flight measurement evaluated against the live control limits.</summary>
public class SpcLivePointRequest
{
    public string Characteristic { get; set; } = "";
    public decimal MeasuredValue { get; set; }
    public int? SubgroupSize { get; set; }
    public string? Plant { get; set; }
    public string? MaterialCode { get; set; }
}

/// <summary>A run-rule violation attributed to the live point.</summary>
public class SpcLiveViolation
{
    public string Rule { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>Result of evaluating one live measurement against the historical control limits.</summary>
public class SpcLiveEvaluation
{
    public string Characteristic { get; set; } = "";
    public int FoundPoints { get; set; }
    public int PointIndex { get; set; }
    public double Xbar { get; set; }
    public double Ucl { get; set; }
    public double Lcl { get; set; }
    public double RBar { get; set; }
    public double RUcl { get; set; }
    public double RLcl { get; set; }
    public bool InControl { get; set; }
    public List<SpcLiveViolation> Violations { get; set; } = new();
    public double? Cp { get; set; }
    public double? Cpk { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Statistical Process Control (SPC) engine.
/// Builds X̄ / R / S control charts, process capability indices and
/// Nelson + Western Electric run-rule violations for inspection results.
/// Read-only: never writes to the database or the audit log.
/// </summary>
public interface ISpcEngineService
{
    /// <summary>
    /// Runs the full SPC analysis (subgroups, charts, capability, violations)
    /// for the given tenant-scoped filter. Empty data yields an empty DTO with
    /// a null capability and no violations.
    /// </summary>
    Task<SpcAnalysisDto> AnalyzeAsync(SpcQueryDto query, CancellationToken ct = default);

    /// <summary>
    /// Distinct characteristics, material codes and plants available to the
    /// current tenant (used to populate the dashboard filter bar).
    /// </summary>
    Task<SpcFilterOptionsDto> GetFilterOptionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Evaluates one live measurement against the control limits derived from the
    /// characteristic's historical inspection results. Insufficient history is
    /// reported through <see cref="SpcLiveEvaluation.Error"/> instead of throwing.
    /// </summary>
    Task<SpcLiveEvaluation> EvaluateLivePointAsync(SpcLivePointRequest request, CancellationToken ct = default);
}
