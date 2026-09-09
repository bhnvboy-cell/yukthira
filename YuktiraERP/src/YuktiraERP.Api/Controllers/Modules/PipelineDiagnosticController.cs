using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PipelineDiagnosticController : ControllerBase
{
    private readonly IPipelineDiagnosticService _diagnosticService;
    private readonly ITenantContext _tenant;

    public PipelineDiagnosticController(IPipelineDiagnosticService diagnosticService, ITenantContext tenant)
    {
        _diagnosticService = diagnosticService;
        _tenant = tenant;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecutePipelineDiagnostic([FromBody] PipelineDiagnosticRequest request)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _diagnosticService.ExecuteFullPipelineDiagnosticAsync(request, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpPost("validate/po")]
    public async Task<IActionResult> ValidatePurchaseOrder([FromQuery] string? poNumber)
    {
        var result = await _diagnosticService.ValidatePurchaseOrderAsync(poNumber, _tenant.TenantId);
        return Ok(result);
    }

    [HttpPost("validate/gr")]
    public async Task<IActionResult> ValidateGoodsReceipt([FromQuery] string? poNumber)
    {
        var result = await _diagnosticService.ValidateGoodsReceiptAsync(poNumber, _tenant.TenantId);
        return Ok(result);
    }

    [HttpPost("validate/inspection-lot")]
    public async Task<IActionResult> ValidateInspectionLot([FromQuery] string? materialCode, [FromQuery] string? batchNumber)
    {
        var result = await _diagnosticService.ValidateInspectionLotAsync(materialCode, batchNumber, _tenant.TenantId);
        return Ok(result);
    }

    [HttpPost("validate/usage-decision")]
    public async Task<IActionResult> ValidateUsageDecision([FromQuery] string? lotNumber)
    {
        var result = await _diagnosticService.ValidateUsageDecisionAsync(lotNumber, _tenant.TenantId);
        return Ok(result);
    }

    [HttpPost("validate/quality-release")]
    public async Task<IActionResult> ValidateQualityRelease([FromQuery] string? lotNumber)
    {
        var result = await _diagnosticService.ValidateQualityReleaseAsync(lotNumber, _tenant.TenantId);
        return Ok(result);
    }

    [HttpPost("validate/delivery")]
    public async Task<IActionResult> ValidateDelivery([FromQuery] string? soNumber)
    {
        var result = await _diagnosticService.ValidateDeliveryAsync(soNumber, _tenant.TenantId);
        return Ok(result);
    }

    [HttpPost("validate/billing")]
    public async Task<IActionResult> ValidateBilling([FromQuery] string? deliveryNumber)
    {
        var result = await _diagnosticService.ValidateBillingDocumentAsync(deliveryNumber, _tenant.TenantId);
        return Ok(result);
    }

    [HttpPost("validate/fi-ledger")]
    public async Task<IActionResult> ValidateFiLedger([FromQuery] string? billingDocNumber)
    {
        var result = await _diagnosticService.ValidateFiLedgerPostingAsync(billingDocNumber, _tenant.TenantId);
        return Ok(result);
    }

    [HttpPost("validate/stock-integrity")]
    public async Task<IActionResult> ValidateStockIntegrity([FromQuery] string? materialCode, [FromQuery] string? batchNumber)
    {
        var result = await _diagnosticService.ValidateStockIntegrityAsync(materialCode, batchNumber, _tenant.TenantId);
        return Ok(result);
    }
}
