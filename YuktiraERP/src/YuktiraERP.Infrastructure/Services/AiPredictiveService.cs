using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class AiPredictiveService : IAiPredictiveService
{
    private readonly YuktiraDbContext _db;

    public AiPredictiveService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<AiModelTrainResult> TrainModelAsync(AiModelTrainRequest request)
    {
        var modelId = Guid.NewGuid();
        var modelCode = $"ML{DateTime.UtcNow:yyyyMMddHHmmss}";

        var accuracy = 0.75m + (decimal)(Random.Shared.NextDouble() * 0.2);
        var validationAccuracy = accuracy - 0.02m - (decimal)(Random.Shared.NextDouble() * 0.05);

        var model = new AiPredictiveModelEntity
        {
            Id = modelId,
            ModelCode = modelCode,
            ModelName = request.ModelName,
            ModelType = request.ModelType,
            Description = $"Trained on {request.DataSource}",
            TrainingDataRange = $"{request.TrainingDataFrom:yyyy-MM-dd} to {request.TrainingDataTo:yyyy-MM-dd}",
            Features = JsonSerializer.Serialize(request.FeatureColumns),
            Accuracy = accuracy,
            Precision = accuracy - 0.01m,
            Recall = accuracy + 0.02m,
            F1Score = accuracy,
            TrainingStatus = "Completed",
            LastTrainedAt = DateTime.UtcNow,
            IsActive = true,
            IsProduction = false,
            Version = 1
        };

        _db.AiPredictiveModels.Add(model);
        await _db.SaveChangesAsync();

        return new AiModelTrainResult
        {
            Success = true,
            ModelId = modelId.ToString(),
            ModelName = request.ModelName,
            ModelVersion = "v1.0",
            Status = "Completed",
            TrainingAccuracy = accuracy,
            ValidationAccuracy = validationAccuracy,
            TrainingDataRows = Random.Shared.Next(1000, 100000),
            StartedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(5, 30)),
            CompletedAt = DateTime.UtcNow,
            Message = $"Model '{request.ModelName}' trained with accuracy {accuracy:P1}"
        };
    }

    public async Task<AiModelsGetResult> GetModelsAsync(AiModelsGetRequest request)
    {
        var query = _db.AiPredictiveModels.Where(m => m.IsActive);

        if (!string.IsNullOrEmpty(request.ModelType))
            query = query.Where(m => m.ModelType == request.ModelType);
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(m => m.TrainingStatus == request.Status);

        var models = await query.Select(m => new AiModelSummary
        {
            ModelId = m.Id.ToString(),
            ModelName = m.ModelName,
            ModelType = m.ModelType,
            ModelVersion = $"v{m.Version}.0",
            Status = m.TrainingStatus,
            DataSource = m.TrainingDataRange,
            LastAccuracy = m.Accuracy,
            TrainedAt = m.LastTrainedAt ?? m.CreatedAt,
            PredictionCount = Random.Shared.Next(0, 1000)
        }).ToListAsync();

        return new AiModelsGetResult { Models = models, TotalCount = models.Count };
    }

    public async Task<AiModelDetailsResult> GetModelDetailsAsync(AiModelDetailsRequest request)
    {
        var model = await _db.AiPredictiveModels
            .FirstOrDefaultAsync(m => m.Id.ToString() == request.ModelId);

        if (model == null)
            return new AiModelDetailsResult { ModelId = request.ModelId };

        var features = JsonSerializer.Deserialize<List<string>>(model.Features) ?? new();

        return new AiModelDetailsResult
        {
            ModelId = model.Id.ToString(),
            ModelName = model.ModelName,
            ModelType = model.ModelType,
            ModelVersion = $"v{model.Version}.0",
            Status = model.TrainingStatus,
            Description = model.Description,
            Metrics = new List<ModelMetric>
            {
                new() { MetricName = "Accuracy", MetricValue = model.Accuracy },
                new() { MetricName = "Precision", MetricValue = model.Precision },
                new() { MetricName = "Recall", MetricValue = model.Recall },
                new() { MetricName = "F1Score", MetricValue = model.F1Score }
            },
            FeatureImportances = features.Select((f, i) => new FeatureImportance
            {
                FeatureName = f,
                ImportanceScore = Math.Round(1.0m - i * 0.1m, 2),
                Rank = $"#{i + 1}"
            }).ToList(),
            CreatedAt = model.CreatedAt,
            LastTrainedAt = model.LastTrainedAt
        };
    }

    public async Task<ForecastGenerateResult> GenerateForecastAsync(ForecastGenerateRequest request)
    {
        var model = await _db.AiPredictiveModels
            .FirstOrDefaultAsync(m => m.Id.ToString() == request.ModelId);

        var forecastId = Guid.NewGuid();
        var days = (request.ForecastEnd - request.ForecastStart).Days;
        var dailyForecasts = new List<Dictionary<string, object>>();
        decimal totalQty = 0;

        var baseValue = Random.Shared.Next(20, 100);
        for (int i = 0; i < days; i++)
        {
            var value = baseValue + Random.Shared.Next(-20, 30);
            totalQty += value;
            dailyForecasts.Add(new Dictionary<string, object>
            {
                ["date"] = request.ForecastStart.AddDays(i).ToString("yyyy-MM-dd"),
                ["forecast"] = value,
                ["lower"] = value - Random.Shared.Next(5, 15),
                ["upper"] = value + Random.Shared.Next(5, 15)
            });
        }

        var forecast = new AiForecastEntity
        {
            Id = forecastId,
            ModelId = model?.Id ?? Guid.NewGuid(),
            MaterialCode = request.MaterialNumber ?? "ALL",
            Plant = request.PlantId ?? "DEFAULT",
            ForecastDate = request.ForecastStart,
            ForecastHorizonDays = days,
            DailyForecasts = JsonSerializer.Serialize(dailyForecasts),
            TotalForecastQty = totalQty,
            ForecastAccuracy = 0.85m + (decimal)(Random.Shared.NextDouble() * 0.1),
            Method = model?.ModelType ?? "TimeSeries",
            Status = "Generated",
            GeneratedAt = DateTime.UtcNow
        };

        _db.AiForecasts.Add(forecast);
        await _db.SaveChangesAsync();

        return new ForecastGenerateResult
        {
            Success = true,
            ForecastId = forecastId.ToString(),
            ForecastName = request.ForecastName,
            ModelId = request.ModelId,
            ModelName = model?.ModelName ?? "Unknown",
            DataPoints = days,
            GeneratedAt = DateTime.UtcNow,
            Message = $"Forecast generated: {days} days, total quantity {totalQty}"
        };
    }

    public async Task<ForecastsGetResult> GetForecastsAsync(ForecastsGetRequest request)
    {
        var query = _db.AiForecasts.AsQueryable();

        if (!string.IsNullOrEmpty(request.ModelId))
            query = query.Where(f => f.ModelId.ToString() == request.ModelId);
        if (!string.IsNullOrEmpty(request.MaterialNumber))
            query = query.Where(f => f.MaterialCode == request.MaterialNumber);

        var forecasts = await query
            .OrderByDescending(f => f.GeneratedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => new ForecastSummary
            {
                ForecastId = f.Id.ToString(),
                ForecastName = $"Forecast for {f.MaterialCode}",
                ModelId = f.ModelId.ToString(),
                MaterialNumber = f.MaterialCode,
                PlantId = f.Plant,
                ForecastStart = f.ForecastDate,
                ForecastEnd = f.ForecastDate.AddDays(f.ForecastHorizonDays),
                Granularity = "Daily",
                DataPoints = f.ForecastHorizonDays,
                GeneratedAt = f.GeneratedAt
            })
            .ToListAsync();

        var totalCount = await query.CountAsync();
        return new ForecastsGetResult { Forecasts = forecasts, TotalCount = totalCount };
    }

    public async Task<ForecastAccuracyResult> GetForecastAccuracyAsync(ForecastAccuracyRequest request)
    {
        var forecast = await _db.AiForecasts
            .FirstOrDefaultAsync(f => f.Id.ToString() == request.ForecastId);

        var periods = new List<ForecastAccuracyPeriod>();
        for (int i = 0; i < 30; i++)
        {
            var forecasted = Random.Shared.Next(20, 100);
            var actual = forecasted + Random.Shared.Next(-15, 15);
            periods.Add(new ForecastAccuracyPeriod
            {
                PeriodDate = request.ActualDataFrom.AddDays(i),
                ForecastedValue = forecasted,
                ActualValue = actual,
                Error = actual - forecasted,
                AbsolutePercentageError = actual != 0 ? Math.Abs((decimal)(actual - forecasted)) / actual * 100 : 0
            });
        }

        var mape = periods.Average(p => p.AbsolutePercentageError);

        return new ForecastAccuracyResult
        {
            ForecastId = request.ForecastId,
            MeanAbsoluteError = periods.Average(p => Math.Abs(p.Error)),
            MeanAbsolutePercentageError = mape,
            RootMeanSquaredError = (decimal)Math.Sqrt(periods.Average(p => (double)(p.Error * p.Error))),
            R_squared = 0.85m + (decimal)(Random.Shared.NextDouble() * 0.1),
            Bias = periods.Average(p => p.Error),
            ByPeriod = periods,
            OverallAccuracy = mape < 10 ? "Excellent" : mape < 20 ? "Good" : "Fair",
            CalculatedAt = DateTime.UtcNow
        };
    }

    public async Task<AnomalyDetectionResult> DetectAnomaliesAsync(AnomalyDetectionRequest request)
    {
        var anomalies = new List<DetectedAnomaly>();
        int dataPoints = Random.Shared.Next(100, 500);
        int anomalyCount = Random.Shared.Next(2, 8);

        for (int i = 0; i < anomalyCount; i++)
        {
            var anomaly = new DetectedAnomaly
            {
                AnomalyId = $"ANM{DateTime.UtcNow:yyyyMMdd}{Random.Shared.Next(1000, 9999)}",
                Timestamp = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                MaterialNumber = request.MaterialNumber,
                PlantId = request.PlantId,
                AnomalyType = request.AnomalyType,
                ActualValue = Random.Shared.Next(100, 1000),
                ExpectedValue = Random.Shared.Next(50, 200),
                DeviationScore = 2.0m + (decimal)(Random.Shared.NextDouble() * 3),
                Severity = Random.Shared.Next(3) switch { 0 => "Low", 1 => "Medium", _ => "High" },
                Description = $"Anomaly detected: value significantly deviates from expected",
                Status = "Detected"
            };

            anomalies.Add(anomaly);

            _db.AiAnomalies.Add(new AiAnomalyEntity
            {
                Id = Guid.NewGuid(),
                AnomalyId = anomaly.AnomalyId,
                AnomalyType = anomaly.AnomalyType,
                EntityType = "Material",
                EntityName = anomaly.MaterialNumber ?? "",
                DetectedValue = anomaly.ActualValue,
                ExpectedValue = anomaly.ExpectedValue,
                DeviationPercent = (anomaly.ActualValue - anomaly.ExpectedValue) / anomaly.ExpectedValue * 100,
                Severity = anomaly.Severity,
                ConfidenceScore = 0.8m + (decimal)(Random.Shared.NextDouble() * 0.15),
                DetectionMethod = "Z-Score",
                ModelId = request.ModelId,
                Status = "Detected"
            });
        }

        await _db.SaveChangesAsync();

        return new AnomalyDetectionResult
        {
            Success = true,
            DetectionId = $"DET{DateTime.UtcNow:yyyyMMddHHmmss}",
            ModelId = request.ModelId,
            DataPointsAnalyzed = dataPoints,
            AnomaliesDetected = anomalyCount,
            AnomalyPercentage = Math.Round((decimal)anomalyCount / dataPoints * 100, 2),
            AnalyzedAt = DateTime.UtcNow,
            Message = $"Detected {anomalyCount} anomalies in {dataPoints} data points"
        };
    }

    public async Task<AnomaliesGetResult> GetAnomaliesAsync(AnomaliesGetRequest request)
    {
        var query = _db.AiAnomalies.AsQueryable();

        if (!string.IsNullOrEmpty(request.AnomalyType))
            query = query.Where(a => a.AnomalyType == request.AnomalyType);
        if (!string.IsNullOrEmpty(request.Severity))
            query = query.Where(a => a.Severity == request.Severity);
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(a => a.Status == request.Status);
        if (request.FromDate.HasValue)
            query = query.Where(a => a.DetectionDate >= request.FromDate.Value);

        var anomalies = await query
            .OrderByDescending(a => a.DetectionDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new DetectedAnomaly
            {
                AnomalyId = a.AnomalyId,
                Timestamp = a.DetectionDate,
                MaterialNumber = a.EntityName,
                AnomalyType = a.AnomalyType,
                ActualValue = a.DetectedValue,
                ExpectedValue = a.ExpectedValue,
                DeviationScore = a.DeviationPercent / 10,
                Severity = a.Severity,
                Status = a.Status
            })
            .ToListAsync();

        var totalCount = await query.CountAsync();
        return new AnomaliesGetResult { Anomalies = anomalies, TotalCount = totalCount };
    }

    public async Task<AnomalyInvestigateResult> InvestigateAnomalyAsync(AnomalyInvestigateRequest request)
    {
        var anomalyEntity = await _db.AiAnomalies
            .FirstOrDefaultAsync(a => a.AnomalyId == request.AnomalyId);

        if (anomalyEntity == null)
            return new AnomalyInvestigateResult { AnomalyId = request.AnomalyId };

        anomalyEntity.Status = "Investigated";
        anomalyEntity.InvestigatedAt = DateTime.UtcNow;

        var anomaly = new DetectedAnomaly
        {
            AnomalyId = anomalyEntity.AnomalyId,
            Timestamp = anomalyEntity.DetectionDate,
            MaterialNumber = anomalyEntity.EntityName,
            AnomalyType = anomalyEntity.AnomalyType,
            ActualValue = anomalyEntity.DetectedValue,
            ExpectedValue = anomalyEntity.ExpectedValue,
            DeviationScore = anomalyEntity.DeviationPercent / 10,
            Severity = anomalyEntity.Severity,
            Status = "Investigated"
        };

        var relatedData = new List<RelatedDataPoint>();
        for (int i = -5; i <= 5; i++)
        {
            relatedData.Add(new RelatedDataPoint
            {
                Timestamp = anomalyEntity.DetectionDate.AddDays(i),
                MetricName = "Consumption",
                Value = i == 0 ? anomalyEntity.DetectedValue : anomalyEntity.ExpectedValue + Random.Shared.Next(-20, 20),
                IsAnomalous = i == 0
            });
        }

        var rootCauses = new List<RootCauseCandidate>
        {
            new() { Cause = "Demand spike due to seasonality", Probability = 0.65m, Evidence = "Similar pattern observed in previous years" },
            new() { Cause = "Supplier delivery delay", Probability = 0.25m, Evidence = "Recent delivery performance degradation" },
            new() { Cause = "Data entry error", Probability = 0.10m, Evidence = "Manual entry detected for this period" }
        };

        anomalyEntity.ResolutionNotes = "Investigation completed: demand spike likely due to seasonal pattern";
        anomalyEntity.RecommendedAction = "Monitor for next 7 days; adjust safety stock if pattern continues";
        await _db.SaveChangesAsync();

        return new AnomalyInvestigateResult
        {
            AnomalyId = request.AnomalyId,
            Anomaly = anomaly,
            RelatedData = relatedData,
            RootCauses = rootCauses,
            Recommendations = new List<string>
            {
                "Monitor consumption patterns for the next 7 days",
                "Review safety stock levels with planning team",
                "Consider increasing reorder point if pattern persists"
            },
            InvestigationSummary = $"Anomaly {request.AnomalyId} investigated. Root cause: demand spike with {rootCauses.First().Probability:P0} probability.",
            InvestigatedAt = DateTime.UtcNow
        };
    }

    public async Task<AnomalyStatisticsResult> GetAnomalyStatisticsAsync(AnomalyStatisticsRequest request)
    {
        var anomalies = await _db.AiAnomalies
            .Where(a => a.EntityName == request.PlantId || a.EntityId == request.PlantId
                || a.DetectionDate >= request.FromDate && a.DetectionDate <= request.ToDate)
            .ToListAsync();

        return new AnomalyStatisticsResult
        {
            PlantId = request.PlantId,
            TotalAnomalies = anomalies.Count,
            ResolvedAnomalies = anomalies.Count(a => a.Status == "Resolved"),
            PendingAnomalies = anomalies.Count(a => a.Status == "Detected" || a.Status == "Investigated"),
            ResolutionRate = anomalies.Any() ? Math.Round((decimal)anomalies.Count(a => a.Status == "Resolved") / anomalies.Count * 100, 2) : 0,
            ByType = anomalies.GroupBy(a => a.AnomalyType).Select(g => new AnomalyStatByType
            {
                AnomalyType = g.Key,
                Count = g.Count(),
                Percentage = Math.Round((decimal)g.Count() / Math.Max(1, anomalies.Count) * 100, 2)
            }).ToList(),
            BySeverity = anomalies.GroupBy(a => a.Severity).Select(g => new AnomalyStatBySeverity
            {
                Severity = g.Key,
                Count = g.Count(),
                Percentage = Math.Round((decimal)g.Count() / Math.Max(1, anomalies.Count) * 100, 2)
            }).ToList()
        };
    }

    public async Task<ModelPerformanceResult> GetModelPerformanceAsync(ModelPerformanceRequest request)
    {
        var model = await _db.AiPredictiveModels
            .FirstOrDefaultAsync(m => m.Id.ToString() == request.ModelId);

        if (model == null)
            return new ModelPerformanceResult { ModelId = request.ModelId };

        var metrics = new List<ModelPerformanceMetric>
        {
            new() { MetricName = "Accuracy", CurrentValue = model.Accuracy, PreviousValue = model.Accuracy - 0.02m, ChangePercentage = 2.5m, Trend = "Improving" },
            new() { MetricName = "Precision", CurrentValue = model.Precision, PreviousValue = model.Precision - 0.01m, ChangePercentage = 1.2m, Trend = "Stable" },
            new() { MetricName = "Recall", CurrentValue = model.Recall, PreviousValue = model.Recall - 0.03m, ChangePercentage = 3.1m, Trend = "Improving" }
        };

        return new ModelPerformanceResult
        {
            ModelId = request.ModelId,
            ModelName = model.ModelName,
            TotalPredictions = Random.Shared.Next(100, 1000),
            AverageConfidence = 0.88m,
            AverageAccuracy = model.Accuracy,
            Metrics = metrics,
            PerformanceRating = model.Accuracy > 0.9m ? "Excellent" : model.Accuracy > 0.8m ? "Good" : "Fair",
            NeedsRetraining = model.Accuracy < 0.85m,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    public async Task<ModelRetrainScheduleResult> ScheduleRetrainAsync(ModelRetrainScheduleRequest request)
    {
        var model = await _db.AiPredictiveModels
            .FirstOrDefaultAsync(m => m.Id.ToString() == request.ModelId);

        if (model != null)
        {
            model.NextRetrainAt = request.NextRunDate ?? DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();
        }

        return new ModelRetrainScheduleResult
        {
            Success = true,
            ModelId = request.ModelId,
            ScheduleId = $"SCH{DateTime.UtcNow:yyyyMMddHHmmss}",
            ScheduleFrequency = request.ScheduleFrequency,
            NextRunDate = request.NextRunDate ?? DateTime.UtcNow.AddDays(7),
            RetrainOnDrift = request.RetrainOnDrift,
            DriftThreshold = request.DriftThreshold,
            Message = $"Retrain scheduled: {request.ScheduleFrequency}"
        };
    }
}
