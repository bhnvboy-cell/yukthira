using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Kpi;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IKpiService _kpiService;
    private readonly IPredictabilityService _predictabilityService;
    private readonly IAIEngine _aiEngine;

    public IndexModel(IKpiService kpiService, IPredictabilityService predictabilityService, IAIEngine aiEngine)
    {
        _kpiService = kpiService;
        _predictabilityService = predictabilityService;
        _aiEngine = aiEngine;
    }

    public KpiCardDto CurrentKpi { get; set; } = new();
    public List<KpiCardDto> History { get; set; } = new();
    public string KpiCode { get; set; } = "";
    public string KpiName { get; set; } = "";
    public string KpiCategory { get; set; } = "";
    public List<AiPredictionDto> AiPredictions { get; set; } = new();
    public bool IsAiLoaded { get; set; }

    public async Task<IActionResult> OnGetAsync(string code)
    {
        KpiCode = code;
        var tenantId = Guid.Empty; // placeholder - extracted from tenant context in real app

        CurrentKpi = await _kpiService.GetDrillDownKpiAsync(tenantId, code);
        KpiName = CurrentKpi.Label;
        KpiCategory = CurrentKpi.Category;

        History = await _kpiService.GetHistoricalKpiSnapshotsAsync(tenantId, code, 30);

        try
        {
            AiPredictions = await GenerateAiPredictionsAsync(code, tenantId);
            IsAiLoaded = true;
        }
        catch
        {
            IsAiLoaded = false;
        }

        return Page();
    }

    private async Task<List<AiPredictionDto>> GenerateAiPredictionsAsync(string kpiCode, Guid tenantId)
    {
        var predictions = new List<AiPredictionDto>();

        // Generate synthetic historical data from recent KPI snapshots
        var history = await _kpiService.GetHistoricalKpiSnapshotsAsync(tenantId, kpiCode, 30);
        var historicalValues = history.Select(h => h.Value).ToList();
        if (historicalValues.Count < 3)
            historicalValues = GenerateSampleData(kpiCode);

        try
        {
            var forecast = await _aiEngine.ForecastAsync(historicalValues, 5, ForecastModel.ExponentialSmoothing, 3);
            var anomalies = await _aiEngine.DetectAnomaliesAsync(historicalValues, AnomalyDetectionMethod.ZScore, 2.0);

            predictions.Add(new AiPredictionDto
            {
                Label = "Next Period Forecast",
                Value = forecast.NextValue.ToString("N1"),
                Unit = CurrentKpi.Unit,
                RiskLevel = "low",
                Description = $"Predicted {CurrentKpi.Label.ToLower()} for next period"
            });

            predictions.Add(new AiPredictionDto
            {
                Label = "Anomalies Detected",
                Value = anomalies.Count.ToString(),
                Unit = "events",
                RiskLevel = anomalies.Count > 2 ? "high" : anomalies.Count > 0 ? "moderate" : "low",
                Description = $"{anomalies.Count} unusual patterns in recent data"
            });

            predictions.Add(new AiPredictionDto
            {
                Label = "Forecast Accuracy (MAPE)",
                Value = forecast.Mape.HasValue ? $"{forecast.Mape:F1}%" : "N/A",
                Unit = "%",
                RiskLevel = (forecast.Mape ?? 100) < 10 ? "low" : (forecast.Mape ?? 100) < 25 ? "moderate" : "high",
                Description = "Model accuracy based on historical fit"
            });

            predictions.Add(new AiPredictionDto
            {
                Label = "30-Day Trend",
                Value = forecast.ForecastedValues.Count > 0
                    ? (forecast.ForecastedValues.Last() - historicalValues.Last()).ToString("+0;-0;0")
                    : "0",
                Unit = CurrentKpi.Unit,
                RiskLevel = forecast.ForecastedValues.Count > 0 && forecast.ForecastedValues.Last() < historicalValues.Last() * 0.8m ? "high"
                    : forecast.ForecastedValues.Count > 0 && forecast.ForecastedValues.Last() < historicalValues.Last() * 0.95m ? "moderate" : "low",
                Description = "Projected change over next 30 days"
            });

            // Add domain-specific predictions
            switch (kpiCode)
            {
                case "InventoryHealth":
                    var stockAlerts = await _predictabilityService.GetStockAlertsAsync(tenantId);
                    predictions.Add(new AiPredictionDto
                    {
                        Label = "Stockout Risk",
                        Value = stockAlerts.Count.ToString(),
                        Unit = "items",
                        RiskLevel = stockAlerts.Count > 5 ? "high" : stockAlerts.Count > 2 ? "moderate" : "low",
                        Description = stockAlerts.Count > 0
                            ? $"{stockAlerts.Count} materials at risk of stockout. Reorder recommended."
                            : "Stock levels within safe range."
                    });
                    break;
                case "QcPassRate":
                    predictions.Add(new AiPredictionDto
                    {
                        Label = "QC Failure Prediction",
                        Value = forecast.NextValue < 90 ? "ELEVATED" : "NORMAL",
                        Unit = "",
                        RiskLevel = forecast.NextValue < 90 ? "high" : forecast.NextValue < 95 ? "moderate" : "low",
                        Description = forecast.NextValue < 90
                            ? "QC failure rate predicted to exceed threshold. Review inspection parameters."
                            : "QC pass rate within acceptable range."
                    });
                    break;
                case "ProductionEfficiency":
                    predictions.Add(new AiPredictionDto
                    {
                        Label = "Production Delay Risk",
                        Value = forecast.NextValue < 80 ? "HIGH" : "LOW",
                        Unit = "",
                        RiskLevel = forecast.NextValue < 80 ? "high" : forecast.NextValue < 90 ? "moderate" : "low",
                        Description = forecast.NextValue < 80
                            ? "Efficiency trending down. Check capacity and material availability."
                            : "Production efficiency projected to remain stable."
                    });
                    break;
                case "SalesToday":
                    var salesForecast = await _aiEngine.ForecastAsync(historicalValues, 7, ForecastModel.LinearRegression);
                    predictions.Add(new AiPredictionDto
                    {
                        Label = "7-Day Sales Trend",
                        Value = salesForecast.NextValue.ToString("N0"),
                        Unit = CurrentKpi.Unit,
                        RiskLevel = salesForecast.NextValue < historicalValues.Average() * 0.8m ? "high"
                            : salesForecast.NextValue < historicalValues.Average() * 0.95m ? "moderate" : "low",
                        Description = salesForecast.NextValue >= historicalValues.Average()
                            ? "Sales projected to grow. Ensure adequate inventory."
                            : "Sales trending down. Consider promotional activities."
                    });
                    break;
            }
        }
        catch
        {
            // AI Engine unavailable - return basic predictions
        }

        return predictions;
    }

    private static List<decimal> GenerateSampleData(string kpiCode) => kpiCode switch
    {
        "InventoryHealth" => new() { 85, 82, 78, 80, 83, 86, 84, 81, 79, 76, 74, 77 },
        "SalesToday" => new() { 8500, 9200, 7800, 10500, 11200, 9800, 8900, 12500, 11800, 9500, 10200, 10800 },
        "ProductionEfficiency" => new() { 88, 91, 85, 92, 90, 87, 93, 89, 86, 94, 91, 88 },
        "QcPassRate" => new() { 96, 95, 97, 94, 93, 96, 98, 95, 94, 92, 95, 97 },
        "LimsSampleTat" => new() { 18, 22, 20, 28, 24, 19, 21, 26, 23, 20, 17, 25 },
        _ => new() { 100, 102, 98, 105, 101, 99, 103, 97, 106, 100, 104, 96 },
    };
}

public class AiPredictionDto
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Unit { get; set; } = "";
    public string RiskLevel { get; set; } = "low";
    public string Description { get; set; } = "";
}
