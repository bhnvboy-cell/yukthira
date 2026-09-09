using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IPipelineDiagnosticService
{
    Task<PipelineDiagnosticResult> ExecuteFullPipelineDiagnosticAsync(PipelineDiagnosticRequest request, Guid tenantId, string userId);
    Task<PipelineStepResult> ValidatePurchaseOrderAsync(string? poNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateGoodsReceiptAsync(string? poNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateInspectionLotAsync(string? materialCode, string? batchNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateInspectionResultsAsync(string? lotNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateUsageDecisionAsync(string? lotNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateQualityReleaseAsync(string? lotNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateSalesOrderAsync(string? soNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateDeliveryAsync(string? soNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateBillingDocumentAsync(string? deliveryNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateFiLedgerPostingAsync(string? billingDocNumber, Guid tenantId);
    Task<PipelineStepResult> ValidateStockIntegrityAsync(string? materialCode, string? batchNumber, Guid tenantId);
}
