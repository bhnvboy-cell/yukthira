using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class AiModelTrainRequest
    {
        public string ModelName { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public string TargetVariable { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public string? DataSourceQuery { get; set; }
        public List<string> FeatureColumns { get; set; } = new();
        public DateTime TrainingDataFrom { get; set; }
        public DateTime TrainingDataTo { get; set; }
        public decimal TrainingSplitPercentage { get; set; } = 0.8m;
        public string? Hyperparameters { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class AiModelTrainResult
    {
        public bool Success { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TrainingAccuracy { get; set; }
        public decimal ValidationAccuracy { get; set; }
        public int TrainingDataRows { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AiModelsGetRequest
    {
        public string? ModelType { get; set; }
        public string? Status { get; set; }
        public bool ActiveOnly { get; set; } = true;
        public string? DataSource { get; set; }
    }

    public class AiModelsGetResult
    {
        public List<AiModelSummary> Models { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class AiModelSummary
    {
        public string ModelId { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public decimal LastAccuracy { get; set; }
        public DateTime TrainedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public int PredictionCount { get; set; }
    }

    public class AiModelDetailsRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public bool IncludeMetrics { get; set; } = true;
        public bool IncludeFeatureImportance { get; set; } = true;
        public bool IncludeTrainingHistory { get; set; } = false;
    }

    public class AiModelDetailsResult
    {
        public string ModelId { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ModelMetric> Metrics { get; set; } = new();
        public List<FeatureImportance> FeatureImportances { get; set; } = new();
        public List<TrainingRun> TrainingHistory { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTrainedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class ModelMetric
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal MetricValue { get; set; }
        public string? MetricUnit { get; set; }
    }

    public class FeatureImportance
    {
        public string FeatureName { get; set; } = string.Empty;
        public decimal ImportanceScore { get; set; }
        public string Rank { get; set; } = string.Empty;
    }

    public class TrainingRun
    {
        public string RunId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal Accuracy { get; set; }
        public int TrainingRows { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ForecastGenerateRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public string ForecastName { get; set; } = string.Empty;
        public DateTime ForecastStart { get; set; }
        public DateTime ForecastEnd { get; set; }
        public string Granularity { get; set; } = string.Empty;
        public string? MaterialNumber { get; set; }
        public string? PlantId { get; set; }
        public string? CustomerId { get; set; }
        public bool IncludeConfidenceIntervals { get; set; } = true;
        public int ConfidenceLevel { get; set; } = 95;
        public Dictionary<string, string>? AdditionalParameters { get; set; }
    }

    public class ForecastGenerateResult
    {
        public bool Success { get; set; }
        public string ForecastId { get; set; } = string.Empty;
        public string ForecastName { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int DataPoints { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ForecastsGetRequest
    {
        public string? ModelId { get; set; }
        public string? ForecastName { get; set; }
        public string? MaterialNumber { get; set; }
        public string? PlantId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ForecastsGetResult
    {
        public List<ForecastSummary> Forecasts { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class ForecastSummary
    {
        public string ForecastId { get; set; } = string.Empty;
        public string ForecastName { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string? MaterialNumber { get; set; }
        public string? PlantId { get; set; }
        public DateTime ForecastStart { get; set; }
        public DateTime ForecastEnd { get; set; }
        public string Granularity { get; set; } = string.Empty;
        public int DataPoints { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class ForecastAccuracyRequest
    {
        public string ForecastId { get; set; } = string.Empty;
        public DateTime ActualDataFrom { get; set; }
        public DateTime ActualDataTo { get; set; }
        public bool CalculateByPeriod { get; set; } = true;
    }

    public class ForecastAccuracyResult
    {
        public string ForecastId { get; set; } = string.Empty;
        public decimal MeanAbsoluteError { get; set; }
        public decimal MeanAbsolutePercentageError { get; set; }
        public decimal RootMeanSquaredError { get; set; }
        public decimal R_squared { get; set; }
        public decimal Bias { get; set; }
        public List<ForecastAccuracyPeriod> ByPeriod { get; set; } = new();
        public string OverallAccuracy { get; set; } = string.Empty;
        public DateTime CalculatedAt { get; set; }
    }

    public class ForecastAccuracyPeriod
    {
        public DateTime PeriodDate { get; set; }
        public decimal ForecastedValue { get; set; }
        public decimal ActualValue { get; set; }
        public decimal Error { get; set; }
        public decimal AbsolutePercentageError { get; set; }
    }

    public class AnomalyDetectionRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public string DataSource { get; set; } = string.Empty;
        public DateTime AnalysisFrom { get; set; }
        public DateTime AnalysisTo { get; set; }
        public string? MaterialNumber { get; set; }
        public string? PlantId { get; set; }
        public decimal? Sensitivity { get; set; }
        public int? MinDataPoints { get; set; }
        public string AnomalyType { get; set; } = string.Empty;
    }

    public class AnomalyDetectionResult
    {
        public bool Success { get; set; }
        public string DetectionId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public int DataPointsAnalyzed { get; set; }
        public int AnomaliesDetected { get; set; }
        public decimal AnomalyPercentage { get; set; }
        public List<DetectedAnomaly> Anomalies { get; set; } = new();
        public DateTime AnalyzedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DetectedAnomaly
    {
        public string AnomalyId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? MaterialNumber { get; set; }
        public string? PlantId { get; set; }
        public string AnomalyType { get; set; } = string.Empty;
        public decimal ActualValue { get; set; }
        public decimal ExpectedValue { get; set; }
        public decimal DeviationScore { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AnomaliesGetRequest
    {
        public string? DetectionId { get; set; }
        public string? AnomalyType { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
        public string? MaterialNumber { get; set; }
        public string? PlantId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AnomaliesGetResult
    {
        public List<DetectedAnomaly> Anomalies { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class AnomalyInvestigateRequest
    {
        public string AnomalyId { get; set; } = string.Empty;
        public bool IncludeRelatedData { get; set; } = true;
        public bool RunRootCauseAnalysis { get; set; } = true;
        public DateTime? ContextWindowFrom { get; set; }
        public DateTime? ContextWindowTo { get; set; }
    }

    public class AnomalyInvestigateResult
    {
        public string AnomalyId { get; set; } = string.Empty;
        public DetectedAnomaly Anomaly { get; set; } = new();
        public List<RelatedDataPoint> RelatedData { get; set; } = new();
        public List<RootCauseCandidate> RootCauses { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public string InvestigationSummary { get; set; } = string.Empty;
        public DateTime InvestigatedAt { get; set; }
    }

    public class RelatedDataPoint
    {
        public DateTime Timestamp { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public bool IsAnomalous { get; set; }
        public string? Remarks { get; set; }
    }

    public class RootCauseCandidate
    {
        public string Cause { get; set; } = string.Empty;
        public decimal Probability { get; set; }
        public string Evidence { get; set; } = string.Empty;
        public string? AffectedEntity { get; set; }
    }

    public class AnomalyStatisticsRequest
    {
        public string PlantId { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? GroupBy { get; set; }
    }

    public class AnomalyStatisticsResult
    {
        public string PlantId { get; set; } = string.Empty;
        public int TotalAnomalies { get; set; }
        public int ResolvedAnomalies { get; set; }
        public int PendingAnomalies { get; set; }
        public decimal ResolutionRate { get; set; }
        public decimal AverageDetectionTime { get; set; }
        public List<AnomalyStatByType> ByType { get; set; } = new();
        public List<AnomalyStatBySeverity> BySeverity { get; set; } = new();
        public List<AnomalyStatByDate> ByDate { get; set; } = new();
    }

    public class AnomalyStatByType
    {
        public string AnomalyType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public decimal AverageSeverity { get; set; }
    }

    public class AnomalyStatBySeverity
    {
        public string Severity { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class AnomalyStatByDate
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public int Resolved { get; set; }
    }

    public class ModelPerformanceRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? MetricGroup { get; set; }
    }

    public class ModelPerformanceResult
    {
        public string ModelId { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int TotalPredictions { get; set; }
        public decimal AverageConfidence { get; set; }
        public decimal AverageAccuracy { get; set; }
        public List<ModelPerformanceMetric> Metrics { get; set; } = new();
        public List<ModelPerformanceTrend> Trends { get; set; } = new();
        public string PerformanceRating { get; set; } = string.Empty;
        public bool NeedsRetraining { get; set; }
        public DateTime AnalyzedAt { get; set; }
    }

    public class ModelPerformanceMetric
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal ChangePercentage { get; set; }
        public string Trend { get; set; } = string.Empty;
    }

    public class ModelPerformanceTrend
    {
        public DateTime PeriodDate { get; set; }
        public decimal Accuracy { get; set; }
        public decimal Confidence { get; set; }
        public int PredictionCount { get; set; }
    }

    public class ModelRetrainScheduleRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public string ScheduleFrequency { get; set; } = string.Empty;
        public DateTime? NextRunDate { get; set; }
        public bool RetrainOnDrift { get; set; } = true;
        public decimal DriftThreshold { get; set; } = 0.05m;
        public bool RetrainOnSchedule { get; set; } = true;
        public Dictionary<string, string>? RetrainingParameters { get; set; }
    }

    public class ModelRetrainScheduleResult
    {
        public bool Success { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public string ScheduleId { get; set; } = string.Empty;
        public string ScheduleFrequency { get; set; } = string.Empty;
        public DateTime NextRunDate { get; set; }
        public bool RetrainOnDrift { get; set; }
        public decimal DriftThreshold { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public interface IAiPredictiveService
    {
        Task<AiModelTrainResult> TrainModelAsync(AiModelTrainRequest request);
        Task<AiModelsGetResult> GetModelsAsync(AiModelsGetRequest request);
        Task<AiModelDetailsResult> GetModelDetailsAsync(AiModelDetailsRequest request);
        Task<ForecastGenerateResult> GenerateForecastAsync(ForecastGenerateRequest request);
        Task<ForecastsGetResult> GetForecastsAsync(ForecastsGetRequest request);
        Task<ForecastAccuracyResult> GetForecastAccuracyAsync(ForecastAccuracyRequest request);
        Task<AnomalyDetectionResult> DetectAnomaliesAsync(AnomalyDetectionRequest request);
        Task<AnomaliesGetResult> GetAnomaliesAsync(AnomaliesGetRequest request);
        Task<AnomalyInvestigateResult> InvestigateAnomalyAsync(AnomalyInvestigateRequest request);
        Task<AnomalyStatisticsResult> GetAnomalyStatisticsAsync(AnomalyStatisticsRequest request);
        Task<ModelPerformanceResult> GetModelPerformanceAsync(ModelPerformanceRequest request);
        Task<ModelRetrainScheduleResult> ScheduleRetrainAsync(ModelRetrainScheduleRequest request);
    }
}
