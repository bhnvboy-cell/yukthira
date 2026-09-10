using YuktiraERP.Core.Enums;

namespace YuktiraERP.Core.Dtos;

// ZQM-01: Auto Inspection Lot Generator DTOs
public class InspectionLotAutoGenRequest
{
    public Guid TenantId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public decimal Quantity { get; set; }
    public string BaseUOM { get; set; } = "EA";
    public string VendorCode { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string PurchaseOrderNumber { get; set; } = "";
    public string PurchaseOrderItem { get; set; } = "";
    public int MovementType { get; set; }
    public string UserId { get; set; } = "";
}

public class InspectionLotAutoGenResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string LotNumber { get; set; } = "";
    public Guid LotId { get; set; }
    public string InspectionType { get; set; } = "";
    public string Origin { get; set; } = "";
    public int SampleSize { get; set; }
    public string AssignedInspector { get; set; } = "";
}

// ZQM-02: Results Recording Workbench DTOs
public class ResultRecordingRequest
{
    public Guid TenantId { get; set; }
    public string LotNumber { get; set; } = "";
    public string Characteristic { get; set; } = "";
    public string MICType { get; set; } = "Quantitative";
    public decimal TargetValue { get; set; }
    public decimal? LSL { get; set; }
    public decimal? USL { get; set; }
    public decimal MeasuredValue { get; set; }
    public string MeasuredUnit { get; set; } = "";
    public string Evaluation { get; set; } = "";
    public string InspectorId { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class ResultRecordingResponse
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Evaluation { get; set; } = "";
    public bool IsWithinSpec { get; set; }
    public decimal Deviation { get; set; }
    public string DeviationPercent { get; set; } = "";
    public string ResultStatus { get; set; } = "";
}

public class WorkbenchLotInfo
{
    public Guid LotId { get; set; }
    public string LotNumber { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Plant { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string InspectionType { get; set; } = "";
    public int SampleSize { get; set; }
    public int ResultsRecorded { get; set; }
    public int ResultsPending { get; set; }
    public string Status { get; set; } = "";
    public List<CharacteristicInfo> Characteristics { get; set; } = new();
}

public class CharacteristicInfo
{
    public Guid Id { get; set; }
    public string Characteristic { get; set; } = "";
    public string MICType { get; set; } = "Quantitative";
    public decimal TargetValue { get; set; }
    public decimal? LSL { get; set; }
    public decimal? USL { get; set; }
    public string Unit { get; set; } = "";
    public bool IsRecorded { get; set; }
    public decimal? MeasuredValue { get; set; }
    public string Evaluation { get; set; } = "";
}

// ZQM-03: Usage Decision Engine DTOs
public class UsageDecisionEngineRequest
{
    public Guid TenantId { get; set; }
    public string LotNumber { get; set; } = "";
    public string UDCode { get; set; } = "";
    public string UDDescription { get; set; } = "";
    public decimal UnrestrictedQty { get; set; }
    public decimal BlockedQty { get; set; }
    public decimal ScrapQty { get; set; }
    public decimal ReworkQty { get; set; }
    public string QualityScoreMethod { get; set; } = "SingleFigure";
    public string UserId { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class UsageDecisionEngineResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string UDCode { get; set; } = "";
    public decimal QualityScore { get; set; }
    public decimal UnrestrictedPosted { get; set; }
    public decimal BlockedPosted { get; set; }
    public decimal ScrapPosted { get; set; }
    public decimal ReworkPosted { get; set; }
    public string StockMovementType { get; set; } = "";
    public string LotStatus { get; set; } = "";
}

// ZQM-04: UD Reversal Engine DTOs
public class UdReversalEngineRequest
{
    public Guid TenantId { get; set; }
    public Guid InspectionLotId { get; set; }
    public string ReversalReason { get; set; } = "";
    public string MovementType { get; set; } = "322";
    public string UserId { get; set; } = "";
    public string Notes { get; set; } = "";
}

// ZQM-05: Handling Unit DTOs
public class HandlingUnitCreateRequest
{
    public Guid TenantId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string WarehouseNumber { get; set; } = "";
    public string PackagingMaterial { get; set; } = "";
    public decimal Quantity { get; set; }
    public string BaseUOM { get; set; } = "EA";
    public decimal GrossWeight { get; set; }
    public decimal NetWeight { get; set; }
    public string WeightUOM { get; set; } = "KG";
    public decimal Volume { get; set; }
    public string VolumeUOM { get; set; } = "L";
    public string PackageType { get; set; } = "Standard";
    public string InspectionLotNumber { get; set; } = "";
    public string UserId { get; set; } = "";
}

public class HandlingUnitResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string HUNumber { get; set; } = "";
    public Guid HUId { get; set; }
    public string Status { get; set; } = "";
    public string WarehouseQueueUrl { get; set; } = "";
}

// ZQM-06: QA32/QA33 Worklist DTOs
public class QaWorklistFilter
{
    public string? PlantFrom { get; set; }
    public string? PlantTo { get; set; }
    public string? MaterialFrom { get; set; }
    public string? MaterialTo { get; set; }
    public string? BatchNumber { get; set; }
    public string? InspectionType { get; set; }
    public string? Status { get; set; }
    public string? AssignedInspector { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public bool PendingUDOnly { get; set; }
    public int MaxHits { get; set; } = 100;
    public string ViewMode { get; set; } = "QA32";
}

// ZQM-07: Non-Conformance DTOs
public class NonConformanceCreateRequest
{
    public Guid TenantId { get; set; }
    public string NCType { get; set; } = "Defect";
    public string Severity { get; set; } = "Minor";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string Plant { get; set; } = "";
    public string InspectionLotNumber { get; set; } = "";
    public string DefectCodeGroup { get; set; } = "";
    public string DefectCode { get; set; } = "";
    public string DefectDescription { get; set; } = "";
    public string RootCauseCategory { get; set; } = "";
    public string RootCauseDescription { get; set; } = "";
    public string DetectedBy { get; set; } = "";
    public string VendorCode { get; set; } = "";
    public decimal AffectedQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public string Priority { get; set; } = "Medium";
    public string AssignedTo { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public string UserId { get; set; } = "";
}

public class NonConformanceResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string NCNumber { get; set; } = "";
    public Guid NCId { get; set; }
    public string Status { get; set; } = "";
    public bool ContainmentTriggered { get; set; }
}

public class CapaCreateRequest
{
    public Guid TenantId { get; set; }
    public string CPType { get; set; } = "Corrective";
    public string NonConformanceNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string RootCauseCategory { get; set; } = "";
    public string RootCauseAnalysis { get; set; } = "";
    public string CorrectiveAction { get; set; } = "";
    public string PreventiveAction { get; set; } = "";
    public string ResponsiblePerson { get; set; } = "";
    public DateTime? PlannedCompletionDate { get; set; }
    public string VerificationMethod { get; set; } = "";
    public string UserId { get; set; } = "";
}

public class CapaResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string CPNumber { get; set; } = "";
    public Guid CPId { get; set; }
    public string Status { get; set; } = "";
}

// ZQM-08: Lab Calculator DTOs
public class LabCalculationRequest
{
    public Guid TenantId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string Plant { get; set; } = "";
    public string InspectionLotNumber { get; set; } = "";
    public string CalculationType { get; set; } = "";
    public decimal RawValue { get; set; }
    public string InputUnit { get; set; } = "";
    public decimal LSL { get; set; }
    public decimal USL { get; set; }
    public string AnalyzedBy { get; set; } = "";
    public string Method { get; set; } = "";
    public string InstrumentId { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class LabCalculationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string AnalysisNumber { get; set; } = "";
    public decimal ConvertedValue { get; set; }
    public string OutputUnit { get; set; } = "";
    public bool IsWithinSpec { get; set; }
    public string Evaluation { get; set; } = "";
    public string Formula { get; set; } = "";
    public decimal? DeviationPercent { get; set; }
}

// ZQM-09: CoA Generator DTOs
public class CoaGenerationRequest
{
    public Guid TenantId { get; set; }
    public string InspectionLotNumber { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string DeliveryNoteNumber { get; set; } = "";
    public string MovementDocumentNumber { get; set; } = "";
    public string GeneratedBy { get; set; } = "";
}

public class CoaGenerationResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string CertificateNumber { get; set; } = "";
    public int TotalCharacteristics { get; set; }
    public int PassedCharacteristics { get; set; }
    public int FailedCharacteristics { get; set; }
    public string OverallResult { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}

// ZQM-10: Pipeline Diagnostic DTOs
public class ZqmPipelineTraceRequest
{
    public Guid TenantId { get; set; }
    public string PurchaseOrderNumber { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string SalesOrderNumber { get; set; } = "";
}

public class ZqmPipelineStepResult
{
    public string StepName { get; set; } = "";
    public string StepCategory { get; set; } = "";
    public bool IsValid { get; set; }
    public string Status { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime? Timestamp { get; set; }
    public string DocumentNumber { get; set; } = "";
    public List<string> BrokenReferences { get; set; } = new();
    public string? Resolution { get; set; }
}

public class ZqmPipelineTraceResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ZqmPipelineStepResult> Steps { get; set; } = new();
    public int TotalSteps { get; set; }
    public int PassedSteps { get; set; }
    public int FailedSteps { get; set; }
    public decimal IntegrityScore { get; set; }
}

// DTOs for entity returns (Core cannot reference Infrastructure entities)
public class HandlingUnitDto
{
    public Guid Id { get; set; }
    public string HUNumber { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string Plant { get; set; } = "";
    public string WarehouseNumber { get; set; } = "";
    public decimal Quantity { get; set; }
    public string BaseUOM { get; set; } = "EA";
    public decimal GrossWeight { get; set; }
    public decimal NetWeight { get; set; }
    public string Status { get; set; } = "";
    public string InspectionLotNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class NonConformanceDto
{
    public Guid Id { get; set; }
    public string NCNumber { get; set; } = "";
    public string NCType { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Status { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string Plant { get; set; } = "";
    public string DefectCode { get; set; } = "";
    public string DefectDescription { get; set; } = "";
    public string RootCauseCategory { get; set; } = "";
    public string DetectedBy { get; set; } = "";
    public DateTime DetectedAt { get; set; }
    public decimal AffectedQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public bool IsContainmentActive { get; set; }
    public string Priority { get; set; } = "";
    public string AssignedTo { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CapaDto
{
    public Guid Id { get; set; }
    public string CPNumber { get; set; } = "";
    public string CPType { get; set; } = "";
    public string Status { get; set; } = "";
    public string NCNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string CorrectiveAction { get; set; } = "";
    public string PreventiveAction { get; set; } = "";
    public string ResponsiblePerson { get; set; } = "";
    public DateTime? PlannedCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
    public bool EffectivenessConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LabAnalysisDto
{
    public Guid Id { get; set; }
    public string AnalysisNumber { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string InspectionLotNumber { get; set; } = "";
    public string CalculationType { get; set; } = "";
    public decimal RawValue { get; set; }
    public string InputUnit { get; set; } = "";
    public decimal ConvertedValue { get; set; }
    public string OutputUnit { get; set; } = "";
    public decimal LSL { get; set; }
    public decimal USL { get; set; }
    public bool IsWithinSpec { get; set; }
    public string Evaluation { get; set; } = "";
    public string Formula { get; set; } = "";
    public string AnalyzedBy { get; set; } = "";
    public DateTime AnalyzedAt { get; set; }
}

public class CoaGenerationLogDto
{
    public Guid Id { get; set; }
    public string CertificateNumber { get; set; } = "";
    public string InspectionLotNumber { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public int TotalCharacteristics { get; set; }
    public int PassedCharacteristics { get; set; }
    public int FailedCharacteristics { get; set; }
    public string OverallResult { get; set; } = "";
    public string GeneratedBy { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public string Status { get; set; } = "";
}

// UD Reversal Result (used by both IInspectionResultService and IZqmUdReversalEngineService)
public class UDReversalResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string InspectionLotNumber { get; set; } = "";
    public string PreviousStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public string PreviousUDCode { get; set; } = "";
    public string StockMovementType { get; set; } = "";
    public decimal StockQuantityMoved { get; set; }
    public string StockFromType { get; set; } = "";
    public string StockToType { get; set; } = "";
    public Guid AuditId { get; set; }
    public DateTime ReversalTimestamp { get; set; }
}
