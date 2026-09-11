using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

// ═══ SD Module Gap Services ═══
public interface ICreditManagementService
{
    Task<CreditCheckResult_Dto> CheckCreditAsync(CreditCheckRequest request);
    Task<List<CreditCheckResult_Dto>> GetCreditOverviewAsync(Guid tenantId, string? customerCode = null);
}

public interface ISchedulingAgreementService
{
    Task<SchedulingAgreementResult> CreateAgreementAsync(SchedulingAgreementRequest request);
    Task<SchedulingAgreementResult> ConfirmScheduleLinesAsync(string agreementNumber, List<ScheduleLineDto> lines, Guid tenantId);
    Task<List<ScheduleLineDto>> GetScheduleLinesAsync(string agreementNumber, Guid tenantId);
}

public interface IRevenueRecognitionService
{
    Task<RevenueRecognitionResult> RecognizeRevenueAsync(RevenueRecognitionRequest request);
    Task<RevenueRecognitionResult> GetRecognitionScheduleAsync(string billingDocumentNumber, Guid tenantId);
}

// ═══ PP Module Gap Services ═══
// Note: ICapacityPlanningService already exists in ICapacityPlanningService.cs
public interface ICapacityGapService
{
    Task<CapacityEvaluationResult> EvaluateCapacityAsync(CapacityEvaluationRequest request);
    Task<List<CapacitySegmentDto>> GetCapacityOverviewAsync(string workCenterCode, DateTime from, DateTime to, Guid tenantId);
}

public interface IKanbanService
{
    Task<KanbanBoardResult> GetKanbanBoardAsync(KanbanBoardRequest request);
    Task<bool> TriggerReplenishmentAsync(string binCode, string materialCode, decimal quantity, Guid tenantId);
}

// ═══ WM Module Gap Services ═══
public interface IPutawayStrategyService
{
    Task<PutawayResult> ExecutePutawayAsync(PutawayRequest request);
    Task<List<PutawayResult>> BatchPutawayAsync(IEnumerable<PutawayRequest> requests, Guid tenantId);
}

public interface ICrossDockService
{
    Task<CrossDockResult> EvaluateCrossDockAsync(CrossDockRequest request);
    Task<List<CrossDockResult>> BatchCrossDockAsync(IEnumerable<CrossDockRequest> requests, Guid tenantId);
}

// ═══ FI Module Gap Services ═══
public interface IIntercompanyAccountingService
{
    Task<IntercompanyTransactionResult> PostIntercompanyTransactionAsync(IntercompanyTransactionRequest request);
    Task<List<IntercompanyTransactionResult>> ReconcileIntercompanyAsync(string companyCode, int fiscalYear, int period, Guid tenantId);
}

public interface IWithholdingTaxService
{
    Task<WithholdingTaxResult> CalculateWithholdingTaxAsync(WithholdingTaxRequest request);
    Task<List<WithholdingTaxResult>> GetWithholdingTaxReportAsync(string companyCode, DateTime from, DateTime to, Guid tenantId);
}

public interface IFinancialCloseService
{
    Task<FinancialCloseResult> ClosePeriodAsync(FinancialCloseRequest request);
    Task<FinancialCloseResult> ReopenPeriodAsync(string companyCode, int fiscalYear, int period, Guid tenantId);
    Task<List<FinancialCloseResult>> GetCloseStatusAsync(string companyCode, int fiscalYear, Guid tenantId);
}

// ═══ CO Module Gap Services ═══
public interface IProductCostingService
{
    Task<ProductCostingResult> CalculateProductCostAsync(ProductCostingRequest request);
    Task<List<ProductCostingResult>> BulkCostEstimateAsync(IEnumerable<ProductCostingRequest> requests, Guid tenantId);
}

public interface IProfitabilityAnalysisService
{
    Task<ProfitabilityAnalysisResult> AnalyzeProfitabilityAsync(ProfitabilityAnalysisRequest request);
    Task<List<PaSegmentDto>> GetSegmentAnalysisAsync(string segment, ProfitabilityAnalysisRequest request, Guid tenantId);
}

public interface ITransferPricingService
{
    Task<TransferPricingResult> CalculateTransferPriceAsync(TransferPricingRequest request);
}

// ═══ HR Module Gap Services ═══
public interface IBenefitsService
{
    Task<BenefitsEnrollmentResult> EnrollBenefitAsync(BenefitsEnrollmentRequest request);
    Task<List<BenefitsEnrollmentResult>> GetEmployeeBenefitsAsync(string employeeCode, Guid tenantId);
}

public interface ISuccessionPlanningService
{
    Task<SuccessionPlanResult> CreateSuccessionPlanAsync(SuccessionPlanRequest request);
    Task<List<SuccessionPlanResult>> GetSuccessionPlansAsync(string positionCode, Guid tenantId);
}
