using YuktiraERP.Core.Enums;

namespace YuktiraERP.Core.Dtos;

public class VisionInspectionRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public string InspectionLotNumber { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string ImageContentType { get; set; } = "image/jpeg";
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class VisionInspectionResult
{
    public bool Success { get; set; }
    public VisionSeverity Severity { get; set; }
    public VisionDefectType DetectedDefect { get; set; }
    public float ConfidenceScore { get; set; }
    public string? DefectDescription { get; set; }
    public List<VisionDefectRegion> DefectRegions { get; set; } = new();
    public bool RequiresNonConformance { get; set; }
    public string? RecommendedAction { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class VisionDefectRegion
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public VisionDefectType Type { get; set; }
    public float Confidence { get; set; }
}

public class VisionBatchResult
{
    public bool Success { get; set; }
    public int TotalImages { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int WarningCount { get; set; }
    public float PassRate { get; set; }
    public List<VisionInspectionResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class ModelTrainingRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public List<VisionTrainingImage> TrainingImages { get; set; } = new();
    public float ValidationSplitRatio { get; set; } = 0.2f;
    public int MaxEpochs { get; set; } = 50;
    public float LearningRate { get; set; } = 0.01f;
}

public class VisionTrainingImage
{
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public VisionDefectType Label { get; set; }
    public string? Description { get; set; }
}

public class ModelTrainingResult
{
    public bool Success { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public ModelTrainingStatus Status { get; set; }
    public float TrainingAccuracy { get; set; }
    public float ValidationAccuracy { get; set; }
    public TimeSpan TrainingDuration { get; set; }
    public string? ModelPath { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ModelEvaluationRequest
{
    public Guid TenantId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public List<VisionTrainingImage> TestImages { get; set; } = new();
}

public class ModelEvaluationResult
{
    public bool Success { get; set; }
    public float Accuracy { get; set; }
    public float Precision { get; set; }
    public float Recall { get; set; }
    public float F1Score { get; set; }
    public Dictionary<VisionDefectType, float> PerClassPrecision { get; set; } = new();
    public int TotalTestImages { get; set; }
    public int CorrectPredictions { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class NlQueryRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 100;
    public bool ExplainPlan { get; set; }
}

public class NlQueryResult
{
    public bool Success { get; set; }
    public string TranslatedQuery { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public NlQueryOperation Operation { get; set; }
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount { get; set; }
    public string? ExecutionPlan { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class NlQuerySuggestion
{
    public string Query { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
}
