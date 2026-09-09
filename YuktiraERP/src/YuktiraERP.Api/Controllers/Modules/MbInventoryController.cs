using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Api.Authorization;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/mm/inventory")]
[Authorize]
public class MbInventoryController : ControllerBase
{
    private readonly IInventoryMovementService _movementService;
    private readonly IStockReservationService _reservationService;
    private readonly IInventoryReportingService _reportingService;
    private readonly IInventoryValuationService _valuationService;
    private readonly ITenantContext _tenant;

    public MbInventoryController(
        IInventoryMovementService movementService,
        IStockReservationService reservationService,
        IInventoryReportingService reportingService,
        IInventoryValuationService valuationService,
        ITenantContext tenant)
    {
        _movementService = movementService;
        _reservationService = reservationService;
        _reportingService = reportingService;
        _valuationService = valuationService;
        _tenant = tenant;
    }

    [HttpPost("movements")]
    [RequireTCode("MB1A")]
    public async Task<IActionResult> PostGoodsMovement([FromBody] PostGoodsMovementRequestDto request)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _movementService.PostMovementAsync(request, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpPost("goods-receipt")]
    [RequireTCode("MB01")]
    public async Task<IActionResult> PostGoodsReceipt([FromBody] PostGoodsMovementRequestDto request)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _movementService.PostGoodsReceiptAsync(request, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpPost("goods-issue")]
    [RequireTCode("MB1A")]
    public async Task<IActionResult> PostGoodsIssue([FromBody] PostGoodsMovementRequestDto request)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _movementService.PostGoodsIssueAsync(request, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpPost("transfer-posting")]
    [RequireTCode("MB1B")]
    public async Task<IActionResult> PostTransferPosting([FromBody] PostGoodsMovementRequestDto request)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _movementService.PostTransferPostingAsync(request, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpPost("reverse/{documentNumber}")]
    [RequireTCode("MB1A")]
    public async Task<IActionResult> ReverseDocument(string documentNumber, [FromQuery] string reason = "Reversal")
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _movementService.ReverseMaterialDocumentAsync(documentNumber, reason, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpGet("document/{documentNumber}")]
    public async Task<IActionResult> GetDocument(string documentNumber)
    {
        var doc = await _movementService.GetMaterialDocumentAsync(documentNumber, _tenant.TenantId);
        return doc == null ? NotFound() : Ok(doc);
    }

    [HttpPost("reservations")]
    [RequireTCode("MB21")]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto request)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _reservationService.CreateReservationAsync(request, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpPut("reservations/{reservationNumber}")]
    [RequireTCode("MB22")]
    public async Task<IActionResult> UpdateReservation(string reservationNumber, [FromBody] CreateReservationDto request)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _reservationService.UpdateReservationAsync(reservationNumber, request, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpDelete("reservations/{reservationNumber}")]
    [RequireTCode("MB22")]
    public async Task<IActionResult> DeleteReservation(string reservationNumber, [FromQuery] string reason = "Cancelled")
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _reservationService.DeleteReservationAsync(reservationNumber, reason, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpPost("reservations/{reservationNumber}/issue")]
    [RequireTCode("MB1A")]
    public async Task<IActionResult> IssueAgainstReservation(string reservationNumber, [FromBody] List<CreateReservationLineDto> issuedLines)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        var result = await _reservationService.IssueAgainstReservationAsync(reservationNumber, issuedLines, _tenant.TenantId, userId);
        return Ok(result);
    }

    [HttpGet("reservations")]
    public async Task<IActionResult> GetReservations([FromQuery] string? materialCode, [FromQuery] string? plant)
    {
        var result = await _reservationService.GetReservationsAsync(materialCode, plant, _tenant.TenantId);
        return Ok(result);
    }

    [HttpGet("reports/mb51")]
    [RequireTCode("MB51")]
    public async Task<IActionResult> GetMb51Report([FromQuery] Mb51FilterDto filter)
    {
        var result = await _reportingService.GetMb51DocumentsAsync(filter, _tenant.TenantId);
        return Ok(result);
    }

    [HttpGet("reports/mb52")]
    [RequireTCode("MB52")]
    public async Task<IActionResult> GetMb52Report([FromQuery] Mb52StockOverviewFilterDto filter)
    {
        var result = await _reportingService.GetMb52StockOverviewAsync(filter, _tenant.TenantId);
        return Ok(result);
    }

    [HttpGet("reports/mb5b")]
    [RequireTCode("MB5B")]
    public async Task<IActionResult> GetMb5BReport([FromQuery] string materialCode, [FromQuery] string plant, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        var result = await _reportingService.GetMb5BHistoricalStockAsync(materialCode, plant, fromDate, toDate, _tenant.TenantId);
        return Ok(result);
    }

    [HttpGet("reports/mb5l")]
    [RequireTCode("MB5L")]
    public async Task<IActionResult> GetMb5LReport([FromQuery] string? materialCode, [FromQuery] string? plant)
    {
        var result = await _reportingService.GetMb5LReconciliationAsync(materialCode, plant, _tenant.TenantId);
        return Ok(result);
    }

    [HttpPost("export/excel")]
    public async Task<IActionResult> ExportToExcel([FromBody] MbExcelExportRequestDto request)
    {
        var bytes = await _reportingService.ExportToExcelAsync(request, _tenant.TenantId);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{request.ReportType}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpGet("valuation/map")]
    public async Task<IActionResult> GetMovingAveragePrice([FromQuery] string materialCode, [FromQuery] string plant)
    {
        var map = await _valuationService.CalculateMovingAveragePriceAsync(materialCode, plant, _tenant.TenantId);
        return Ok(new { MaterialCode = materialCode, Plant = plant, MovingAveragePrice = map });
    }

    [HttpPost("valuation/revaluate")]
    [RequireTCode("MB1A")]
    public async Task<IActionResult> RevaluateMaterial([FromQuery] string materialCode, [FromQuery] string plant)
    {
        var userId = User.Identity?.Name ?? "SYSTEM";
        await _valuationService.RevaluateMaterialAsync(materialCode, plant, _tenant.TenantId, userId);
        return Ok(new { Success = true, MaterialCode = materialCode, Plant = plant });
    }
}
