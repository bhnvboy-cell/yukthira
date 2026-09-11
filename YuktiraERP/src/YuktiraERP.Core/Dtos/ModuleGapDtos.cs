using YuktiraERP.Core.Enums;

namespace YuktiraERP.Core.Dtos;

// ══════════════════════════════════════════════════════
// SD: Credit Management
// ══════════════════════════════════════════════════════
public class CreditCheckRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public string SalesOrderNumber { get; set; } = string.Empty;
}

public class CreditCheckResult_Dto
{
    public bool Success { get; set; }
    public CreditCheckResult Result { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal ExposureAfterOrder { get; set; }
    public string? OverrideReason { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// SD: Scheduling Agreement
// ══════════════════════════════════════════════════════
public class SchedulingAgreementRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string AgreementNumber { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public List<ScheduleLineDto> ScheduleLines { get; set; } = new();
}

public class ScheduleLineDto
{
    public DateTime DeliveryDate { get; set; }
    public decimal Quantity { get; set; }
    public ScheduleLineStatus Status { get; set; }
    public decimal DeliveredQuantity { get; set; }
}

public class SchedulingAgreementResult
{
    public bool Success { get; set; }
    public string? AgreementNumber { get; set; }
    public int ScheduleLineCount { get; set; }
    public decimal TotalConfirmedQuantity { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// SD: Revenue Recognition
// ══════════════════════════════════════════════════════
public class RevenueRecognitionRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string BillingDocumentNumber { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime ServiceStartDate { get; set; }
    public DateTime ServiceEndDate { get; set; }
    public string RecognitionMethod { get; set; } = "StraightLine";
}

public class RevenueRecognitionResult
{
    public bool Success { get; set; }
    public string? BillingDocumentNumber { get; set; }
    public string? RecognitionScheduleId { get; set; }
    public decimal RecognizedAmount { get; set; }
    public decimal DeferredAmount { get; set; }
    public RevenueRecognitionStatus Status { get; set; }
    public int PeriodsCount { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// PP: Capacity Planning (CRP)
// ══════════════════════════════════════════════════════
public class CapacityEvaluationRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string WorkCenterCode { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public CapacityCategory Category { get; set; }
}

public class CapacityEvaluationResult
{
    public bool Success { get; set; }
    public string WorkCenterCode { get; set; } = string.Empty;
    public decimal AvailableCapacity { get; set; }
    public decimal AssignedCapacity { get; set; }
    public decimal UtilizationPercent { get; set; }
    public CapacityLoadStatus LoadStatus { get; set; }
    public List<CapacitySegmentDto> Segments { get; set; } = new();
    public List<string> OverloadWarnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class CapacitySegmentDto
{
    public DateTime Date { get; set; }
    public decimal AvailableHours { get; set; }
    public decimal AssignedHours { get; set; }
    public decimal UtilizationPercent { get; set; }
    public string? OverloadedOrder { get; set; }
}

// ══════════════════════════════════════════════════════
// PP: Kanban
// ══════════════════════════════════════════════════════
public class KanbanBoardRequest
{
    public Guid TenantId { get; set; }
    public string Plant { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string ProductionLine { get; set; } = string.Empty;
}

public class KanbanBoardResult
{
    public bool Success { get; set; }
    public List<KanbanBoardItemDto> Items { get; set; } = new();
    public decimal TotalInventory { get; set; }
    public decimal ReplenishmentQuantity { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class KanbanBoardItemDto
{
    public string BinCode { get; set; } = string.Empty;
    public KanbanStatus Status { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal TargetQuantity { get; set; }
    public string? AssignedProductionOrder { get; set; }
}

// ══════════════════════════════════════════════════════
// WM: Putaway Strategy
// ══════════════════════════════════════════════════════
public class PutawayRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public string WarehouseNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public string? BatchNumber { get; set; }
    public PutawayStrategy Strategy { get; set; }
}

public class PutawayResult
{
    public bool Success { get; set; }
    public string? AssignedBin { get; set; }
    public string? Zone { get; set; }
    public decimal BinCapacity { get; set; }
    public decimal RemainingCapacity { get; set; }
    public bool RequiresConsolidation { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// WM: Cross-Docking
// ══════════════════════════════════════════════════════
public class CrossDockRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string SourceDocument { get; set; } = string.Empty;
    public string TargetDocument { get; set; } = string.Empty;
}

public class CrossDockResult
{
    public bool Success { get; set; }
    public bool EligibleForCrossDock { get; set; }
    public string? AssignedBin { get; set; }
    public decimal DemandMatchQuantity { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// FI: Intercompany Accounting
// ══════════════════════════════════════════════════════
public class IntercompanyTransactionRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public IntercompanyTransactionType TransactionType { get; set; }
    public string SendingCompanyCode { get; set; } = string.Empty;
    public string ReceivingCompanyCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
}

public class IntercompanyTransactionResult
{
    public bool Success { get; set; }
    public string? SendingDocumentNumber { get; set; }
    public string? ReceivingDocumentNumber { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal LocalAmount { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// FI: Withholding Tax
// ══════════════════════════════════════════════════════
public class WithholdingTaxRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public string TaxCode { get; set; } = string.Empty;
    public WithholdingTaxType TaxType { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class WithholdingTaxResult
{
    public bool Success { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? TaxCertificateNumber { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// CO: Product Costing
// ══════════════════════════════════════════════════════
public class ProductCostingRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public CostingType CostingType { get; set; }
    public DateTime CostingDate { get; set; }
}

public class ProductCostingResult
{
    public bool Success { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal CostPerUnit { get; set; }
    public List<CostComponentDto> Components { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class CostComponentDto
{
    public string Name { get; set; } = string.Empty;
    public CostComponentCategory Category { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public string CalculationBase { get; set; } = string.Empty;
}

// ══════════════════════════════════════════════════════
// CO: CO-PA (Profitability Analysis)
// ══════════════════════════════════════════════════════
public class ProfitabilityAnalysisRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string OperatingConcern { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? CustomerSegment { get; set; }
    public string? ProductSegment { get; set; }
    public string? Region { get; set; }
}

public class ProfitabilityAnalysisResult
{
    public bool Success { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public decimal OperatingMarginPercent { get; set; }
    public List<PaSegmentDto> Segments { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class PaSegmentDto
{
    public string SegmentName { get; set; } = string.Empty;
    public string SegmentValue { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Costs { get; set; }
    public decimal Margin { get; set; }
    public decimal MarginPercent { get; set; }
}

// ══════════════════════════════════════════════════════
// CO: Transfer Pricing
// ══════════════════════════════════════════════════════
public class TransferPricingRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string SendingCompanyCode { get; set; } = string.Empty;
    public string ReceivingCompanyCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string PricingMethod { get; set; } = "CostPlus";
    public decimal? MarkupPercent { get; set; }
}

public class TransferPricingResult
{
    public bool Success { get; set; }
    public decimal TransferPrice { get; set; }
    public decimal CostBase { get; set; }
    public decimal MarkupAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// HR: Benefits Administration
// ══════════════════════════════════════════════════════
public class BenefitsEnrollmentRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public BenefitType BenefitType { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public decimal EmployeeContribution { get; set; }
    public decimal EmployerContribution { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class BenefitsEnrollmentResult
{
    public bool Success { get; set; }
    public string? EnrollmentId { get; set; }
    public decimal TotalPremium { get; set; }
    public decimal EmployeeShare { get; set; }
    public decimal EmployerShare { get; set; }
    public DateTime CoverageStartDate { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// HR: Succession Planning
// ══════════════════════════════════════════════════════
public class SuccessionPlanRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string PositionCode { get; set; } = string.Empty;
    public string CurrentIncumbentCode { get; set; } = string.Empty;
    public List<SuccessionCandidateDto> Candidates { get; set; } = new();
}

public class SuccessionCandidateDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public SuccessionReadiness Readiness { get; set; }
    public string? DevelopmentPlan { get; set; }
    public int PerformanceRating { get; set; }
    public string? RiskOfLoss { get; set; }
}

public class SuccessionPlanResult
{
    public bool Success { get; set; }
    public string? PlanId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public SuccessionReadiness OverallReadiness { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ══════════════════════════════════════════════════════
// FI: Financial Close
// ══════════════════════════════════════════════════════
public class FinancialCloseRequest
{
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public int Period { get; set; }
    public bool ForceClose { get; set; }
}

public class FinancialCloseResult
{
    public bool Success { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public ClosePeriodStatus Status { get; set; }
    public int OpenItemsCount { get; set; }
    public decimal UnpostedAmount { get; set; }
    public List<string> BlockingIssues { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
