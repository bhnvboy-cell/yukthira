using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Core.Interfaces;

public interface IMovementTypeEngineService
{
    Task<MovementTypeEntity?> GetMovementTypeAsync(int movementType, Guid tenantId);
    Task<List<MovementTypeEntity>> GetAllMovementTypesAsync(Guid tenantId);
    Task<List<MovementTypeEntity>> GetByCategoryAsync(string category, Guid tenantId);
    Task<List<MovementTypeEntity>> GetByStockTypeAsync(string stockType, Guid tenantId);
    Task<MovementValidationResult> ValidateMovementAsync(MovementValidationRequest request);
    Task<bool> IsReversalMovementAsync(int movementType, Guid tenantId);
    Task<int?> GetReversalMovementTypeAsync(int movementType, Guid tenantId);
    Task<List<string>> GetAllowedStockTypesAsync(int movementType, Guid tenantId);
    Task<bool> IsStockTypeCompatibleAsync(int movementType, string stockType, Guid tenantId);
    Task<List<string>> GetCompatibleStockTypesAsync(int movementType, Guid tenantId);
    Task<WorkflowSimulationResult> SimulateWorkflowAsync(MovementSimulationRequest request);
    Task<List<MovementTraceEntry>> GetMovementTraceAsync(Guid documentId);
    Task<MovementPostResult> PostMovementAsync(MovementPostRequest request);
    Task<MovementPostResult> ReverseMovementAsync(Guid documentId, string reason, string userId);
    Task<List<MovementTypeIntegrationEntity>> GetIntegrationFlagsAsync(int movementType, Guid tenantId);
    Task<bool> CheckIntegrationAsync(int movementType, string targetModule, Guid tenantId);
    Task<List<MovementDocumentEntity>> GetDocumentFlowAsync(string reference, string referenceType, Guid tenantId);
    Task<string> GenerateDocumentNumberAsync(Guid tenantId);
    Task<List<MovementTypeCategoryEntity>> GetAllCategoriesAsync(Guid tenantId);
    Task<List<MovementTypeStockTypeEntity>> GetAllStockTypesAsync(Guid tenantId);
}

public class MovementValidationRequest
{
    public int MovementType { get; set; }
    public string SpecialStockIndicator { get; set; } = string.Empty;
    public string StockType { get; set; } = "FREE";
    public string Plant { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}

public class MovementValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Info { get; set; } = new();
    public MovementTypeEntity? MovementType { get; set; }
}

public class MovementSimulationRequest
{
    public int MovementType { get; set; }
    public string SpecialStockIndicator { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Plant { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public string StockType { get; set; } = "FREE";
    public Guid TenantId { get; set; }
}

public class WorkflowSimulationResult
{
    public bool WouldSucceed { get; set; }
    public List<WorkflowStepResult> Steps { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class WorkflowStepResult
{
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string StepType { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal DurationMs { get; set; }
}

public class MovementTraceEntry
{
    public Guid DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public int MovementType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public class MovementPostRequest
{
    public int MovementType { get; set; }
    public string SpecialStockIndicator { get; set; } = string.Empty;
    public string PostingDate { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string HeaderText { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public List<MovementPostLineRequest> Lines { get; set; } = new();
}

public class MovementPostLineRequest
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public string Plant { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public string StockType { get; set; } = "FREE";
    public string VendorCode { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string ProductionOrderNo { get; set; } = string.Empty;
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public string CostCenter { get; set; } = string.Empty;
    public string GLAccount { get; set; } = string.Empty;
    public string ItemText { get; set; } = string.Empty;
}

public class MovementPostResult
{
    public bool Success { get; set; }
    public Guid? DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<MovementTraceEntry> Trace { get; set; } = new();
}
