namespace YuktiraERP.Core.Interfaces;

public class ForecastResult
{
    public string ModelName { get; set; } = string.Empty;
    public List<decimal> HistoricalValues { get; set; } = new();
    public List<decimal> ForecastedValues { get; set; } = new();
    public decimal NextValue { get; set; }
    public double? RSquare { get; set; }
    public double? Mape { get; set; }
}

public enum ForecastModel { MovingAverage, WeightedMovingAverage, ExponentialSmoothing, LinearRegression, SeasonalDecomposition }
public enum AdvancedForecastModel { HoltWinters, Arima, MlNetRegression }
public enum AnomalyDetectionMethod { ZScore, Iqr, MovingAverageDeviation }

public class HoltWintersResult
{
    public string ModelName { get; set; } = "Holt-Winters";
    public List<decimal> ForecastedValues { get; set; } = new();
    public decimal NextValue { get; set; }
    public double Alpha { get; set; }
    public double Beta { get; set; }
    public double Gamma { get; set; }
    public double Mape { get; set; }
}

public class ArimaResult
{
    public string ModelName { get; set; } = "ARIMA";
    public List<decimal> ForecastedValues { get; set; } = new();
    public decimal NextValue { get; set; }
    public int P { get; set; }
    public int D { get; set; }
    public int Q { get; set; }
    public double Aic { get; set; }
    public double Mape { get; set; }
}

public class AnomalyResult
{
    public int Index { get; set; }
    public decimal Value { get; set; }
    public double DeviationScore { get; set; }
    public string Label { get; set; } = "";
}

public class ForecastAccuracyDto
{
    public string ModelName { get; set; } = "";
    public double Mape { get; set; }
    public double Mae { get; set; }
    public double Rmse { get; set; }
    public double RSquared { get; set; }
}

public interface IAIEngine
{
    Task<ForecastResult> ForecastAsync(List<decimal> historicalData, int forecastPeriods, ForecastModel model = ForecastModel.MovingAverage, int period = 3);
    Task<HoltWintersResult> HoltWintersAsync(List<decimal> data, int forecastPeriods, double alpha = 0.3, double beta = 0.1, double gamma = 0.1, int seasonLength = 4);
    Task<ArimaResult> ArimaAsync(List<decimal> data, int forecastPeriods, int p = 1, int d = 1, int q = 1);
    Task<List<AnomalyResult>> DetectAnomaliesAsync(List<decimal> data, AnomalyDetectionMethod method = AnomalyDetectionMethod.ZScore, double threshold = 2.0);
    Task<ForecastAccuracyDto> CalculateAccuracyAsync(List<decimal> actual, List<decimal> forecast);
}
