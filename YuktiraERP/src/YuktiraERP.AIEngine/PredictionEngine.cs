using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.AIEngine;

public class PredictionEngine : IAIEngine
{
    public Task<ForecastResult> ForecastAsync(List<decimal> data, int forecastPeriods, ForecastModel model = ForecastModel.MovingAverage, int period = 3)
    {
        var result = model switch
        {
            ForecastModel.MovingAverage => new ForecastResult
            {
                ModelName = "Moving Average",
                HistoricalValues = data,
                ForecastedValues = MovingAverage(data, period),
                NextValue = MovingAverage(data, period).LastOrDefault()
            },
            ForecastModel.WeightedMovingAverage => new ForecastResult
            {
                ModelName = "Weighted Moving Average",
                HistoricalValues = data,
                ForecastedValues = WeightedMovingAverage(data, period),
                NextValue = WeightedMovingAverage(data, period).LastOrDefault()
            },
            ForecastModel.ExponentialSmoothing => new ForecastResult
            {
                ModelName = "Exponential Smoothing",
                HistoricalValues = data,
                ForecastedValues = ExponentialSmoothing(data, 0.3),
                NextValue = ExponentialSmoothing(data, 0.3).LastOrDefault()
            },
            ForecastModel.LinearRegression => new ForecastResult
            {
                ModelName = "Linear Regression",
                HistoricalValues = data,
                ForecastedValues = LinearRegression(data, forecastPeriods).Forecast,
                NextValue = LinearRegression(data, forecastPeriods).Forecast.LastOrDefault(),
                RSquare = LinearRegression(data, forecastPeriods).RSquared
            },
            ForecastModel.SeasonalDecomposition => new ForecastResult
            {
                ModelName = "Seasonal Decomposition",
                HistoricalValues = data,
                ForecastedValues = SeasonalDecomposition(data, 4, forecastPeriods),
                NextValue = SeasonalDecomposition(data, 4, forecastPeriods).LastOrDefault()
            },
            _ => throw new ArgumentException($"Unknown model: {model}")
        };
        return Task.FromResult(result);
    }

    public Task<HoltWintersResult> HoltWintersAsync(List<decimal> data, int forecastPeriods, double alpha = 0.3, double beta = 0.1, double gamma = 0.1, int seasonLength = 4)
    {
        if (data.Count < seasonLength)
        {
            var fallback = MovingAverage(data, 3);
            return Task.FromResult(new HoltWintersResult
            {
                ForecastedValues = fallback,
                NextValue = fallback.LastOrDefault(),
                Alpha = alpha, Beta = beta, Gamma = gamma
            });
        }

        int n = data.Count;
        var level = new double[n];
        var trend = new double[n];
        var seasonal = new double[n];

        for (int i = 0; i < seasonLength; i++)
            seasonal[i] = (double)data[i];

        level[seasonLength - 1] = (double)data.Take(seasonLength).Average();
        trend[seasonLength - 1] = 0;

        for (int i = seasonLength; i < n; i++)
        {
            double prevLevel = level[i - 1];
            double prevTrend = trend[i - 1];
            double prevSeasonal = seasonal[i - seasonLength];
            double y = (double)data[i];

            level[i] = alpha * (y - prevSeasonal) + (1 - alpha) * (prevLevel + prevTrend);
            trend[i] = beta * (level[i] - prevLevel) + (1 - beta) * prevTrend;
            seasonal[i] = gamma * (y - level[i]) + (1 - gamma) * prevSeasonal;
        }

        var forecasted = new List<decimal>();
        for (int k = 1; k <= forecastPeriods; k++)
        {
            double f = level[n - 1] + k * trend[n - 1] + seasonal[(n - 1 + k) % seasonLength];
            forecasted.Add((decimal)f);
        }

        double mape = CalculateMapeInternal(data.Skip(seasonLength).ToList(), forecasted.Take(n - seasonLength).ToList());
        return Task.FromResult(new HoltWintersResult
        {
            ForecastedValues = forecasted,
            NextValue = forecasted.LastOrDefault(),
            Alpha = alpha, Beta = beta, Gamma = gamma,
            Mape = mape
        });
    }

    public Task<ArimaResult> ArimaAsync(List<decimal> data, int forecastPeriods, int p = 1, int d = 1, int q = 1)
    {
        if (data.Count < 3)
        {
            var fallback = MovingAverage(data, 3);
            return Task.FromResult(new ArimaResult
            {
                ForecastedValues = fallback,
                NextValue = fallback.LastOrDefault(),
                P = p, D = d, Q = q
            });
        }

        var diffData = data.Select(x => (double)x).ToList();
        for (int diff = 0; diff < d; diff++)
        {
            var next = new List<double>();
            for (int i = 1; i < diffData.Count; i++)
                next.Add(diffData[i] - diffData[i - 1]);
            diffData = next;
        }

        int n = diffData.Count;
        if (n < Math.Max(p, q) + 1)
        {
            var fallback = MovingAverage(data, 3);
            return Task.FromResult(new ArimaResult
            {
                ForecastedValues = fallback,
                NextValue = fallback.LastOrDefault(),
                P = p, D = d, Q = q
            });
        }

        var residuals = new double[n];
        var arCoeffs = new double[p];
        if (p > 0 && n > p)
        {
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            int m = n - p;
            for (int i = p; i < n; i++)
            {
                double y = diffData[i];
                double x = diffData[i - 1];
                sumX += x; sumY += y; sumXY += x * y; sumX2 += x * x;
            }
            double denom = m * sumX2 - sumX * sumX;
            if (Math.Abs(denom) > 1e-12)
                arCoeffs[0] = (m * sumXY - sumX * sumY) / denom;
        }

        var arFitted = new double[n];
        for (int i = p; i < n; i++)
        {
            double pred = 0;
            for (int j = 0; j < p; j++)
                pred += arCoeffs[j] * diffData[i - j - 1];
            arFitted[i] = pred;
            residuals[i] = diffData[i] - pred;
        }

        var maCoeffs = new double[q];
        if (q > 0)
        {
            double sumRes = residuals.Skip(n - q).Sum();
            double avgRes = sumRes / q;
            for (int j = 0; j < q; j++)
                maCoeffs[j] = 1.0 / q;
        }

        var forecasted = new List<decimal>();
        var lastValues = diffData.ToList();
        var lastResiduals = residuals.ToList();
        for (int k = 0; k < forecastPeriods; k++)
        {
            double arPred = 0;
            for (int j = 0; j < p && j < lastValues.Count; j++)
                arPred += arCoeffs[j] * lastValues[lastValues.Count - j - 1];

            double maPred = 0;
            for (int j = 0; j < q && j < lastResiduals.Count; j++)
                maPred += maCoeffs[j] * lastResiduals[lastResiduals.Count - j - 1];

            double combined = arPred + maPred;
            forecasted.Add((decimal)combined);
            lastValues.Add(combined);
            lastResiduals.Add(0);
        }

        if (d > 0)
        {
            var undiffed = new List<decimal>();
            double lastOrig = (double)data.Last();
            for (int k = 0; k < forecasted.Count; k++)
            {
                lastOrig = lastOrig + (double)forecasted[k];
                undiffed.Add((decimal)lastOrig);
            }
            forecasted = undiffed;
        }

        int fitCount = Math.Min(n, data.Count - d);
        var actualFit = data.Skip(d).Take(fitCount).ToList();
        var forecastFit = forecasted.Take(fitCount).Select(x => (decimal)x).ToList();
        double mape = CalculateMapeInternal(actualFit, forecastFit);

        return Task.FromResult(new ArimaResult
        {
            ForecastedValues = forecasted,
            NextValue = forecasted.LastOrDefault(),
            P = p, D = d, Q = q,
            Mape = mape
        });
    }

    public Task<List<AnomalyResult>> DetectAnomaliesAsync(List<decimal> data, AnomalyDetectionMethod method = AnomalyDetectionMethod.ZScore, double threshold = 2.0)
    {
        if (data.Count == 0)
            return Task.FromResult(new List<AnomalyResult>());

        var results = new List<AnomalyResult>();

        switch (method)
        {
            case AnomalyDetectionMethod.ZScore:
            {
                double mean = (double)data.Average();
                double std = data.Count > 1
                    ? Math.Sqrt(data.Select(d => Math.Pow((double)d - mean, 2)).Average())
                    : 0;
                for (int i = 0; i < data.Count; i++)
                {
                    double z = std > 0 ? Math.Abs(((double)data[i] - mean) / std) : 0;
                    if (z > threshold)
                    {
                        results.Add(new AnomalyResult
                        {
                            Index = i,
                            Value = data[i],
                            DeviationScore = z,
                            Label = z > threshold * 1.5 ? "Extreme" : "Anomaly"
                        });
                    }
                }
                break;
            }
            case AnomalyDetectionMethod.Iqr:
            {
                var sorted = data.OrderBy(d => d).ToList();
                int n = sorted.Count;
                decimal q1 = sorted[n / 4];
                decimal q3 = sorted[(3 * n) / 4];
                decimal iqr = q3 - q1;
                decimal lower = q1 - 1.5m * iqr;
                decimal upper = q3 + 1.5m * iqr;
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i] < lower || data[i] > upper)
                    {
                        double score = data[i] < lower
                            ? (double)((lower - data[i]) / iqr)
                            : (double)((data[i] - upper) / iqr);
                        results.Add(new AnomalyResult
                        {
                            Index = i,
                            Value = data[i],
                            DeviationScore = score,
                            Label = data[i] < lower ? "Low Extreme" : "High Extreme"
                        });
                    }
                }
                break;
            }
            case AnomalyDetectionMethod.MovingAverageDeviation:
            {
                int window = Math.Max(3, data.Count / 10);
                for (int i = 0; i < data.Count; i++)
                {
                    int start = Math.Max(0, i - window);
                    int count = i - start;
                    if (count < 2) continue;
                    var slice = data.Skip(start).Take(count).ToList();
                    double localMean = (double)slice.Average();
                    double localStd = Math.Sqrt(slice.Select(d => Math.Pow((double)d - localMean, 2)).Average());
                    double dev = localStd > 0 ? Math.Abs(((double)data[i] - localMean) / localStd) : 0;
                    if (dev > threshold)
                    {
                        results.Add(new AnomalyResult
                        {
                            Index = i,
                            Value = data[i],
                            DeviationScore = dev,
                            Label = dev > threshold * 1.5 ? "Extreme" : "Anomaly"
                        });
                    }
                }
                break;
            }
        }

        return Task.FromResult(results);
    }

    public Task<ForecastAccuracyDto> CalculateAccuracyAsync(List<decimal> actual, List<decimal> forecast)
    {
        int n = Math.Min(actual.Count, forecast.Count);
        if (n == 0)
            return Task.FromResult(new ForecastAccuracyDto());

        double sumAbsPctErr = 0, sumAbsErr = 0, sumSqErr = 0;
        double sumActual = 0, sumForecast = 0;
        int validCount = 0;

        for (int i = 0; i < n; i++)
        {
            double a = (double)actual[i];
            double f = (double)forecast[i];
            double absErr = Math.Abs(a - f);
            sumAbsErr += absErr;
            sumSqErr += absErr * absErr;
            sumActual += a;
            sumForecast += f;
            if (a != 0)
            {
                sumAbsPctErr += Math.Abs((a - f) / a);
                validCount++;
            }
        }

        double mape = validCount > 0 ? sumAbsPctErr / validCount * 100 : 0;
        double mae = sumAbsErr / n;
        double rmse = Math.Sqrt(sumSqErr / n);
        double ssRes = sumSqErr;
        double meanActual = sumActual / n;
        double ssTot = actual.Take(n).Sum(a => Math.Pow((double)a - meanActual, 2));
        double rSquared = ssTot > 0 ? 1 - ssRes / ssTot : 0;

        return Task.FromResult(new ForecastAccuracyDto
        {
            Mape = mape,
            Mae = mae,
            Rmse = rmse,
            RSquared = rSquared
        });
    }

    private static double CalculateMapeInternal(List<decimal> actual, List<decimal> forecast)
    {
        int n = Math.Min(actual.Count, forecast.Count);
        if (n == 0) return 0;
        double sum = 0;
        int valid = 0;
        for (int i = 0; i < n; i++)
        {
            double a = (double)actual[i];
            if (a != 0)
            {
                sum += Math.Abs((a - (double)forecast[i]) / a);
                valid++;
            }
        }
        return valid > 0 ? sum / valid * 100 : 0;
    }

    public List<decimal> MovingAverage(List<decimal> data, int period)
    {
        if (data.Count < period) return new List<decimal> { data.Count > 0 ? data.Average() : 0 };
        var result = new List<decimal>();
        for (int i = period; i <= data.Count; i++)
            result.Add(data.Skip(i - period).Take(period).Average());
        return result;
    }

    public List<decimal> WeightedMovingAverage(List<decimal> data, int period)
    {
        if (data.Count < period) return new List<decimal> { data.Count > 0 ? data.Average() : 0 };
        var weights = Enumerable.Range(1, period).Select(w => (decimal)w).ToList();
        var weightSum = weights.Sum();
        var result = new List<decimal>();
        for (int i = period; i <= data.Count; i++)
        {
            var segment = data.Skip(i - period).Take(period).ToList();
            decimal weightedSum = 0;
            for (int j = 0; j < period; j++)
                weightedSum += segment[j] * weights[j];
            result.Add(weightedSum / weightSum);
        }
        return result;
    }

    public List<decimal> ExponentialSmoothing(List<decimal> data, double alpha = 0.3)
    {
        if (data.Count == 0) return new List<decimal>();
        var result = new List<decimal> { data[0] };
        for (int i = 1; i < data.Count; i++)
            result.Add((decimal)(alpha * (double)data[i] + (1 - alpha) * (double)result[i - 1]));
        return result;
    }

    public (List<decimal> Forecast, double RSquared) LinearRegression(List<decimal> data, int forecastPeriods)
    {
        if (data.Count < 2) return (new List<decimal> { data.LastOrDefault() }, 0);
        int n = data.Count;
        double sumX = Enumerable.Range(0, n).Sum(i => (double)i);
        double sumY = data.Sum(d => (double)d);
        double sumXY = Enumerable.Range(0, n).Sum(i => (double)i * (double)data[i]);
        double sumX2 = Enumerable.Range(0, n).Sum(i => (double)i * i);
        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;
        var forecast = Enumerable.Range(n, forecastPeriods).Select(i => (decimal)(slope * i + intercept)).ToList();
        double ssRes = Enumerable.Range(0, n).Sum(i => Math.Pow((double)data[i] - (slope * i + intercept), 2));
        double ssTot = Enumerable.Range(0, n).Sum(i => Math.Pow((double)data[i] - sumY / n, 2));
        double rSquared = ssTot > 0 ? 1 - ssRes / ssTot : 0;
        return (forecast, rSquared);
    }

    public List<decimal> SeasonalDecomposition(List<decimal> data, int seasonLength, int forecastPeriods)
    {
        if (data.Count < seasonLength * 2) return MovingAverage(data, 3);
        var seasonalFactors = new List<decimal>();
        for (int i = 0; i < seasonLength; i++)
        {
            var values = new List<decimal>();
            for (int j = i; j < data.Count; j += seasonLength)
                values.Add(data[j]);
            seasonalFactors.Add(values.Average());
        }
        var grandAvg = seasonalFactors.Average();
        var seasonalIndexes = seasonalFactors.Select(s => s / grandAvg).ToList();
        var deseasonalized = data.Select((v, i) => v / seasonalIndexes[i % seasonLength]).ToList();
        var trend = LinearRegression(deseasonalized, forecastPeriods).Forecast;
        var result = new List<decimal>();
        for (int i = 0; i < forecastPeriods; i++)
            result.Add(trend[i] * seasonalIndexes[(data.Count + i) % seasonLength]);
        return result;
    }
}
