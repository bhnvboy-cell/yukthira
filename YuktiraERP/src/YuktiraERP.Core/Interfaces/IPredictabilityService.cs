namespace YuktiraERP.Core.Interfaces;

public class SafetyStockResult
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal AverageDemand { get; set; }
    public double DemandStdDev { get; set; }
    public decimal LeadTimeDays { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal DaysOfStock { get; set; }
    public string Recommendation { get; set; } = "";
}

public class DemandForecastDto
{
    public string MaterialName { get; set; } = "";
    public string ModelName { get; set; } = "";
    public List<decimal> HistoricalDemand { get; set; } = new();
    public List<decimal> ForecastedDemand { get; set; } = new();
    public List<string> PeriodLabels { get; set; } = new();
    public decimal NextPeriodForecast { get; set; }
    public double RSquare { get; set; }
    public double Mape { get; set; }
}

public interface IPredictabilityService
{
    Task<DemandForecastDto> ForecastDemandAsync(Guid tenantId, Guid materialId, int forecastPeriods, ForecastModel model = ForecastModel.ExponentialSmoothing);
    Task<SafetyStockResult> CalculateSafetyStockAsync(Guid tenantId, Guid materialId, double serviceLevel = 0.95, decimal leadTimeDays = 7);
    Task<List<SafetyStockResult>> GetStockAlertsAsync(Guid tenantId, double serviceLevel = 0.95);
    Task<List<SafetyStockResult>> CalculateAllSafetyStockAsync(Guid tenantId, double serviceLevel = 0.95, decimal defaultLeadTimeDays = 7);
}
