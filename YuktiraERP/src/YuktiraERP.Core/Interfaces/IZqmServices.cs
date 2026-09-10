using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IZqmAutoLotGeneratorService
{
    Task<InspectionLotAutoGenResult> GenerateInspectionLotAsync(InspectionLotAutoGenRequest request);
    Task<bool> IsQmEnabledMaterialAsync(string materialCode, string plant, Guid tenantId);
}

public interface IZqmResultsWorkbenchService
{
    Task<WorkbenchLotInfo?> GetLotForRecordingAsync(string lotNumber, Guid tenantId);
    Task<ResultRecordingResponse> RecordCharacteristicResultAsync(ResultRecordingRequest request);
    Task<List<CharacteristicInfo>> GetRecordedResultsAsync(string lotNumber);
    Task<bool> CompleteResultsRecordingAsync(string lotNumber, string userId);
}

public interface IZqmUsageDecisionEngineService
{
    Task<UsageDecisionEngineResult> PostUsageDecisionAsync(UsageDecisionEngineRequest request);
    Task<bool> ValidateUdCompletenessAsync(string lotNumber, Guid tenantId);
}

public interface IZqmUdReversalEngineService
{
    Task<UDReversalResult> ReverseUdAsync(UdReversalEngineRequest request);
}

public interface IZqmHandlingUnitService
{
    Task<HandlingUnitResult> CreateHandlingUnitAsync(HandlingUnitCreateRequest request);
    Task<HandlingUnitResult> ReleaseHandlingUnitAsync(Guid huId, string userId);
    Task<List<HandlingUnitDto>> GetHandlingUnitsByLotAsync(string lotNumber, Guid tenantId);
    Task<List<HandlingUnitDto>> GetPackagingQueueAsync(string warehouseNumber, Guid tenantId);
}

public interface IZqmQaWorklistService
{
    Task<List<InspectionLotSelectionResultDto>> GetWorklistAsync(QaWorklistFilter filter, Guid tenantId);
    Task<int> GetWorklistCountAsync(QaWorklistFilter filter, Guid tenantId);
}

public interface IZqmNonConformanceService
{
    Task<NonConformanceResult> CreateNonConformanceAsync(NonConformanceCreateRequest request);
    Task<CapaResult> CreateCapaAsync(CapaCreateRequest request);
    Task<bool> ActivateContainmentAsync(Guid ncId, string action, DateTime? expiry, string userId);
    Task<bool> CompleteCapaAsync(Guid cpId, string userId, string notes);
    Task<bool> VerifyCapaAsync(Guid cpId, string userId, bool effective, string notes);
    Task<List<NonConformanceDto>> GetNonConformancesAsync(Guid tenantId, string? status = null);
    Task<List<CapaDto>> GetCapasAsync(Guid tenantId, string? status = null);
}

public interface IZqmLabCalculatorService
{
    Task<LabCalculationResult> CalculateAsync(LabCalculationRequest request);
    Task<List<LabAnalysisDto>> GetAnalysisByLotAsync(string lotNumber, Guid tenantId);
    decimal CalculateMoistureContent(decimal wetBasis, decimal dryBasis);
    decimal CalculateDrySubstance(decimal moisture);
    decimal CalculateStarchPurity(decimal starch, decimal drySubstance);
    decimal CalculateGrainDefect(decimal defectiveWeight, decimal totalWeight);
    decimal CalculateBaumeGravity(decimal density);
    decimal CalculateDEValue(decimal reducingSugar, decimal totalDrySolids);
}

public interface IZqmCoaGeneratorService
{
    Task<CoaGenerationResult> GenerateCoaAsync(CoaGenerationRequest request);
    Task<List<CoaGenerationLogDto>> GetCoaLogsAsync(Guid tenantId, string? materialCode = null);
}

public interface IZqmPipelineDiagnosticService
{
    Task<ZqmPipelineTraceResult> TraceFullPipelineAsync(ZqmPipelineTraceRequest request);
    Task<ZqmPipelineStepResult> ValidateInspectionLotStepAsync(string lotNumber, Guid tenantId);
    Task<ZqmPipelineStepResult> ValidateResultsStepAsync(string lotNumber, Guid tenantId);
    Task<ZqmPipelineStepResult> ValidateUsageDecisionStepAsync(string lotNumber, Guid tenantId);
    Task<ZqmPipelineStepResult> ValidateQualityReleaseStepAsync(string lotNumber, Guid tenantId);
    Task<ZqmPipelineStepResult> ValidateCoaStepAsync(string lotNumber, Guid tenantId);
}
