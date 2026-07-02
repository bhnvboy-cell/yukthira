using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIEngineController : ControllerBase
{
    private readonly IAIEngine _aiEngine;
    private readonly IPredictabilityService _predictability;
    private readonly ITenantContext _tenant;

    public AIEngineController(IAIEngine aiEngine, IPredictabilityService predictability, ITenantContext tenant)
    {
        _aiEngine = aiEngine;
        _predictability = predictability;
        _tenant = tenant;
    }

    [HttpPost("forecast")]
    public async Task<IActionResult> Forecast([FromBody] ForecastApiRequest request)
    {
        var result = await _aiEngine.ForecastAsync(request.HistoricalData, request.ForecastPeriods, request.Model, request.Period);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("demand-prediction/{materialId}")]
    public async Task<IActionResult> DemandPrediction(Guid materialId, [FromQuery] int periods = 6)
    {
        var result = await _predictability.ForecastDemandAsync(_tenant.TenantId, materialId, periods);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("stock-prediction/{materialId}")]
    public async Task<IActionResult> StockPrediction(Guid materialId, [FromQuery] int periods = 6)
    {
        var result = await _predictability.CalculateSafetyStockAsync(_tenant.TenantId, materialId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("holt-winters")]
    public async Task<IActionResult> HoltWinters([FromBody] HoltWintersRequest request)
    {
        var result = await _aiEngine.HoltWintersAsync(request.Data, request.ForecastPeriods, request.Alpha, request.Beta, request.Gamma, request.SeasonLength);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("arima")]
    public async Task<IActionResult> Arima([FromBody] ArimaRequest request)
    {
        var result = await _aiEngine.ArimaAsync(request.Data, request.ForecastPeriods, request.P, request.D, request.Q);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("anomalies")]
    public async Task<IActionResult> Anomalies([FromBody] AnomalyRequest request)
    {
        var result = await _aiEngine.DetectAnomaliesAsync(request.Data, request.Method, request.Threshold);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("accuracy")]
    public async Task<IActionResult> Accuracy([FromBody] AccuracyRequest request)
    {
        var result = await _aiEngine.CalculateAccuracyAsync(request.Actual, request.Forecast);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("forecast-dashboard/{materialId}")]
    public async Task<IActionResult> ForecastDashboard(Guid materialId, [FromQuery] int periods = 6)
    {
        var demandForecast = await _predictability.ForecastDemandAsync(_tenant.TenantId, materialId, periods);
        var safetyStock = await _predictability.CalculateSafetyStockAsync(_tenant.TenantId, materialId);
        var anomalies = await _aiEngine.DetectAnomaliesAsync(demandForecast.HistoricalDemand, AnomalyDetectionMethod.ZScore);
        return Ok(new
        {
            data = new
            {
                DemandForecast = demandForecast,
                SafetyStock = safetyStock,
                Anomalies = anomalies
            },
            tenantId = _tenant.TenantId
        });
    }
}

public class ForecastApiRequest
{
    public List<decimal> HistoricalData { get; set; } = new();
    public int ForecastPeriods { get; set; } = 3;
    public ForecastModel Model { get; set; } = ForecastModel.MovingAverage;
    public int Period { get; set; } = 3;
}

public class HoltWintersRequest
{
    public List<decimal> Data { get; set; } = new();
    public int ForecastPeriods { get; set; } = 4;
    public double Alpha { get; set; } = 0.3;
    public double Beta { get; set; } = 0.1;
    public double Gamma { get; set; } = 0.1;
    public int SeasonLength { get; set; } = 4;
}

public class ArimaRequest
{
    public List<decimal> Data { get; set; } = new();
    public int ForecastPeriods { get; set; } = 4;
    public int P { get; set; } = 1;
    public int D { get; set; } = 1;
    public int Q { get; set; } = 1;
}

public class AnomalyRequest
{
    public List<decimal> Data { get; set; } = new();
    public AnomalyDetectionMethod Method { get; set; } = AnomalyDetectionMethod.ZScore;
    public double Threshold { get; set; } = 2.0;
}

public class AccuracyRequest
{
    public List<decimal> Actual { get; set; } = new();
    public List<decimal> Forecast { get; set; } = new();
}
