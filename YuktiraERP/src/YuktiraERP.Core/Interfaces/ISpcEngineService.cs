using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

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
}
