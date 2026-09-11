using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IQualityVisionInspectionEngine
{
    Task<VisionInspectionResult> InspectImageAsync(VisionInspectionRequest request);
    Task<VisionBatchResult> InspectBatchAsync(IEnumerable<VisionInspectionRequest> images, Guid tenantId);
    Task<ModelTrainingResult> TrainModelAsync(ModelTrainingRequest request);
    Task<ModelEvaluationResult> EvaluateModelAsync(ModelEvaluationRequest request);
}

public interface INaturalLanguageQueryEngine
{
    Task<NlQueryResult> ExecuteQueryAsync(NlQueryRequest request);
    Task<List<NlQuerySuggestion>> GetSuggestionsAsync(string partialQuery, Guid tenantId);
}
