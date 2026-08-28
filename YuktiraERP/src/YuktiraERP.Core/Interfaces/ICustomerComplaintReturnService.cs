namespace YuktiraERP.Core.Interfaces;

/// <summary>
/// Customer Complaint & Return with Supplier Pass-Through Claim lifecycle service.
/// Orchestrates SD-QM-MM-FI cross-functional workflow.
/// </summary>
public interface ICustomerComplaintReturnService
{
    // CR-01: Create Customer Complaint & Return Order
    Task<ComplaintReturnResult> CreateComplaintAndReturnOrderAsync(ComplaintReturnRequest request);

    // CR-02: Post Return Delivery & Goods Receipt (Mvt 651)
    Task<ReturnDeliveryResult> PostReturnDeliveryAsync(ReturnDeliveryRequest request);

    // CR-03: Record Quality Inspection Root-Cause Analysis
    Task<QualityInspectionResult> RecordInspectionResultsAsync(QualityInspectionRequest request);

    // CR-04: Post Usage Decision (Reject → Blocked Stock)
    Task<UsageDecisionResult> PostUsageDecisionAsync(UsageDecisionRequest request);

    // CR-05: Issue Customer Credit Memo
    Task<CreditMemoResult> IssueCreditMemoAsync(CreditMemoRequest request);

    // CR-06: Create Supplier Complaint
    Task<SupplierClaimResult> CreateSupplierComplaintAsync(SupplierClaimRequest request);

    // CR-07: Post Supplier Return Delivery (Mvt 122)
    Task<SupplierReturnResult> PostSupplierReturnDeliveryAsync(SupplierReturnRequest request);

    // CR-08: Issue Supplier Credit Recovery (Debit Memo)
    Task<DebitMemoResult> IssueDebitMemoAsync(DebitMemoRequest request);

    // Workflow orchestration
    Task<ComplaintReturnResult> ExecuteFullWorkflowAsync(ComplaintReturnRequest request);
    Task<List<ComplaintWorkflowStep>> GetWorkflowProgressAsync(Guid complaintReturnId);
    Task<ComplaintReturnResult> GetComplaintDetailsAsync(Guid complaintReturnId);
}

// ══════════════════════════════════════════════════════════════════════════════
// Request / Response DTOs
// ══════════════════════════════════════════════════════════════════════════════

public class ComplaintReturnRequest
{
    public Guid TenantId { get; set; }
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal ReturnQuantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public string BatchNumber { get; set; } = "";
    public string DefectCode { get; set; } = "";
    public string DefectDescription { get; set; } = "";
    public string DefectCategory { get; set; } = "";
    public string SupplierVendorCode { get; set; } = "";
    public string SupplierVendorName { get; set; } = "";
    public string SupplierBatchNumber { get; set; } = "";
    public string PurchaseOrderReference { get; set; } = "";
    public string Plant { get; set; } = "PLT-01";
    public string StorageLocation { get; set; } = "SL-01";
    public string CostCenter { get; set; } = "";
    public string ProfitCenter { get; set; } = "";
    public string Priority { get; set; } = "Medium";
    public string Notes { get; set; } = "";
}

public class ReturnDeliveryRequest
{
    public Guid ComplaintReturnId { get; set; }
    public string DeliveryNumber { get; set; } = "";
    public decimal Quantity { get; set; }
    public string BatchNumber { get; set; } = "";
    public string Plant { get; set; } = "PLT-01";
    public string StorageLocation { get; set; } = "SL-01";
    public string PostingDate { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class QualityInspectionRequest
{
    public Guid ComplaintReturnId { get; set; }
    public string InspectionLotNumber { get; set; } = "";
    public string Characteristic { get; set; } = "";
    public string Specification { get; set; } = "";
    public string ResultValue { get; set; } = "";
    public string ResultValuation { get; set; } = "";
    public string DefectCodeGroup { get; set; } = "";
    public string DefectCode { get; set; } = "";
    public string DefectDescription { get; set; } = "";
    public string DefectCategory { get; set; } = "";
    public string RootCause { get; set; } = "";
    public string RootCauseCode { get; set; } = "";
    public string RecordedBy { get; set; } = "";
}

public class UsageDecisionRequest
{
    public Guid ComplaintReturnId { get; set; }
    public string UsageDecision { get; set; } = "R";
    public string UsageDecisionCode { get; set; } = "";
    public string StockProposal { get; set; } = "";
    public string TargetStockType { get; set; } = "BLOCKED";
    public string DecidedBy { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class CreditMemoRequest
{
    public Guid ComplaintReturnId { get; set; }
    public string BillingType { get; set; } = "RE";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string CostCenter { get; set; } = "";
    public string ProfitCenter { get; set; } = "";
    public string GLAccount { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class SupplierClaimRequest
{
    public Guid ComplaintReturnId { get; set; }
    public string VendorCode { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string SupplierBatchNumber { get; set; } = "";
    public string PurchaseOrderNumber { get; set; } = "";
    public decimal ClaimQuantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal ClaimAmount { get; set; }
    public decimal UnitPrice { get; set; } = 0;
    public string DefectCode { get; set; } = "";
    public string DefectDescription { get; set; } = "";
    public string DefectCategory { get; set; } = "";
    public string RootCause { get; set; } = "";
    public string RootCauseCode { get; set; } = "";
    public string Plant { get; set; } = "PLT-01";
    public string CostCenter { get; set; } = "";
    public string ProfitCenter { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class SupplierReturnRequest
{
    public Guid SupplierClaimId { get; set; }
    public string DeliveryNumber { get; set; } = "";
    public decimal Quantity { get; set; }
    public string BatchNumber { get; set; } = "";
    public string VendorCode { get; set; } = "";
    public string PurchaseOrderNumber { get; set; } = "";
    public string Plant { get; set; } = "PLT-01";
    public string StorageLocation { get; set; } = "SL-01";
    public string PostingDate { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class DebitMemoRequest
{
    public Guid SupplierClaimId { get; set; }
    public string VendorCode { get; set; } = "";
    public string VendorName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string PurchaseOrderReference { get; set; } = "";
    public string CostCenter { get; set; } = "";
    public string ProfitCenter { get; set; } = "";
    public string GLAccount { get; set; } = "";
    public string Notes { get; set; } = "";
}

// ══════════════════════════════════════════════════════════════════════════════
// Result DTOs
// ══════════════════════════════════════════════════════════════════════════════

public class ComplaintReturnResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Guid ComplaintReturnId { get; set; }
    public string ComplaintNumber { get; set; } = "";
    public string ReturnOrderNumber { get; set; } = "";
    public string QualityNotificationNumber { get; set; } = "";
    public decimal ReturnAmount { get; set; }
    public string Status { get; set; } = "";
    public string CurrentStep { get; set; } = "";
}

public class ReturnDeliveryResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Guid ReturnDeliveryId { get; set; }
    public string MaterialDocumentNumber { get; set; } = "";
    public string InspectionLotNumber { get; set; } = "";
    public int MovementType { get; set; }
    public string StockType { get; set; } = "";
}

public class QualityInspectionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Guid InspectionReturnId { get; set; }
    public string RootCause { get; set; } = "";
    public string DefectCode { get; set; } = "";
    public bool IsSupplierDefect { get; set; }
}

public class UsageDecisionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Guid InspectionReturnId { get; set; }
    public string UsageDecision { get; set; } = "";
    public string StockType { get; set; } = "";
    public bool RequiresSupplierClaim { get; set; }
}

public class CreditMemoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string CreditMemoNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string DocumentNumber { get; set; } = "";
}

public class SupplierClaimResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Guid SupplierClaimId { get; set; }
    public string SupplierClaimNumber { get; set; } = "";
    public decimal ClaimAmount { get; set; }
    public string QualityNotificationNumber { get; set; } = "";
}

public class SupplierReturnResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Guid SupplierReturnDeliveryId { get; set; }
    public string MaterialDocumentNumber { get; set; } = "";
    public int MovementType { get; set; }
    public decimal Quantity { get; set; }
}

public class DebitMemoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string DebitMemoNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string DocumentNumber { get; set; } = "";
}

public class ComplaintWorkflowStep
{
    public string StepName { get; set; } = "";
    public string StepCode { get; set; } = "";
    public int StepOrder { get; set; }
    public string Module { get; set; } = "";
    public string TransactionCode { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? CompletedAt { get; set; }
    public bool IsCurrentStep { get; set; }
}
