namespace YuktiraERP.Core.Interfaces;

public class KpiDefinitionDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Formula { get; set; } = "";
    public string Category { get; set; } = "";
    public string Unit { get; set; } = "";
    public string? Trend { get; set; }
}

public class KpiCardDto
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public string DisplayValue { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Icon { get; set; } = "bi-graph-up";
    public string Category { get; set; } = "";
    public string Color { get; set; } = "green";
    public string Trend { get; set; } = "flat";
    public decimal TrendValue { get; set; }
    public string TrendLabel { get; set; } = "";
    public decimal? PreviousValue { get; set; }
    public string? AlertMessage { get; set; }
    public string DrillDownUrl { get; set; } = "";
}

public class KpiBadgeDto
{
    public string ModuleCode { get; set; } = "";
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "green";
    public string Trend { get; set; } = "flat";
}

public interface IKpiService
{
    Task<Dictionary<string, decimal>> CalculateKpiAsync(Guid tenantId, string kpiCode);
    Task<List<KpiDefinitionDto>> GetAvailableKpisAsync(Guid tenantId);
    Task<List<KpiCardDto>> GetDashboardKpisAsync(Guid tenantId);
    Task<List<KpiBadgeDto>> GetModuleKpiBadgesAsync(Guid tenantId);
    Task<KpiCardDto> GetDrillDownKpiAsync(Guid tenantId, string kpiCode);
    Task<List<KpiCardDto>> GetHistoricalKpiSnapshotsAsync(Guid tenantId, string kpiCode, int days = 30);
    Task<Dictionary<string, decimal>> GetThresholdsAsync();
    Task<string> GetKpiColorCodeAsync(decimal value, decimal? thresholdGreen, decimal? thresholdYellow);
}
