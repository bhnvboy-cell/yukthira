namespace YuktiraERP.Core.Dtos;

public enum PipelineStepStatus
{
    Pending,
    Passed,
    Failed,
    Warning,
    Skipped,
    NotApplicable
}

public enum PipelineStepCategory
{
    MM_Procurement,
    MM_GoodsReceipt,
    MM_InventoryMovement,
    QM_InspectionLot,
    QM_InspectionResults,
    QM_UsageDecision,
    QM_QualityRelease,
    SD_SalesOrder,
    SD_Delivery,
    SD_Billing,
    FI_GeneralLedger,
    CrossCutting_TransactionIntegrity
}

public class PipelineStepResult
{
    public int StepNumber { get; set; }
    public string StepName { get; set; } = "";
    public string Description { get; set; } = "";
    public PipelineStepCategory Category { get; set; }
    public PipelineStepStatus Status { get; set; } = PipelineStepStatus.Pending;
    public string? DocumentNumber { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Amount { get; set; }
    public string? MovementType { get; set; }
    public string? StockType { get; set; }
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Resolution { get; set; }
    public long DurationMs { get; set; }
    public List<string> ValidationChecks { get; set; } = new();
    public List<string> BrokenReferences { get; set; } = new();
}

public class PipelineDiagnosticResult
{
    public Guid DiagnosticId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public long TotalDurationMs { get; set; }
    public int TotalSteps { get; set; }
    public int PassedSteps { get; set; }
    public int FailedSteps { get; set; }
    public int WarningSteps { get; set; }
    public int SkippedSteps { get; set; }
    public PipelineStepStatus OverallStatus { get; set; }
    public List<PipelineStepResult> Steps { get; set; } = new();
    public List<string> CriticalIssues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public PipelineTraceabilityInfo? Traceability { get; set; }
}

public class PipelineTraceabilityInfo
{
    public string? PurchaseOrderNumber { get; set; }
    public string? GoodsReceiptNumber { get; set; }
    public string? InspectionLotNumber { get; set; }
    public string? UsageDecisionId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? BillingDocumentNumber { get; set; }
    public string? FiJournalDocumentNumber { get; set; }
    public decimal? TotalInvoiceAmount { get; set; }
    public decimal? TotalFiPostedAmount { get; set; }
    public bool IsBalanced { get; set; }
}

public class PipelineDiagnosticRequest
{
    public Guid? TenantId { get; set; }
    public string? MaterialCode { get; set; }
    public string? BatchNumber { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? SalesOrderNumber { get; set; }
    public bool AutoFix { get; set; } = false;
    public bool IncludeSkipped { get; set; } = true;
}
