using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/qm/vision-gate")]
[Authorize]
public class VisionQmController : ControllerBase
{
    private readonly IVisionQmGateService _service;
    private readonly ITenantContext _tenant;

    public VisionQmController(IVisionQmGateService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpPost("frame")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Frame([FromBody] VisionQmFrameRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return BadRequest(new { error = "imageBase64 and materialCode are required" });
        }

        request.TenantId = _tenant.TenantId;
        request.UserId = GetUserId().ToString();

        var result = await _service.ProcessFrameAsync(request, HttpContext.RequestAborted);

        if (result.Errors.Count > 0 && !result.Passed && string.IsNullOrEmpty(result.MaterialDocumentNumber) && string.IsNullOrEmpty(result.NonConformanceId))
        {
            return BadRequest(new
            {
                error = string.Join("; ", result.Errors),
                passed = result.Passed,
                grade = result.Grade,
                impurityPct = result.ImpurityPct,
                defectType = result.DefectType,
                severity = result.Severity,
                confidence = result.Confidence,
                materialDocumentNumber = result.MaterialDocumentNumber,
                inspectionLotNumber = result.InspectionLotNumber,
                nonConformanceId = result.NonConformanceId
            });
        }

        return Ok(new
        {
            passed = result.Passed,
            grade = result.Grade,
            impurityPct = result.ImpurityPct,
            defectType = result.DefectType,
            severity = result.Severity,
            confidence = result.Confidence,
            materialDocumentNumber = result.MaterialDocumentNumber,
            inspectionLotNumber = result.InspectionLotNumber,
            nonConformanceId = result.NonConformanceId,
            errors = result.Errors
        });
    }

    [HttpPost("frame/upload")]
    [Authorize(Policy = "PowerUserOrAbove")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> FrameUpload(
        [FromForm] IFormFile image,
        [FromForm] string materialCode,
        [FromForm] string plant,
        [FromForm] decimal quantity,
        [FromForm] string unit,
        [FromForm] string? batchNumber,
        [FromForm] string? vendorCode,
        [FromForm] string? materialName)
    {
        if (image is null || image.Length == 0)
        {
            return BadRequest(new { error = "image file is required" });
        }

        if (string.IsNullOrWhiteSpace(materialCode))
        {
            return BadRequest(new { error = "materialCode is required" });
        }

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms, HttpContext.RequestAborted);

        var request = new VisionQmFrameRequest
        {
            TenantId = _tenant.TenantId,
            UserId = GetUserId().ToString(),
            ImageBase64 = Convert.ToBase64String(ms.ToArray()),
            MaterialCode = materialCode,
            MaterialName = materialName ?? materialCode,
            Plant = string.IsNullOrWhiteSpace(plant) ? "1000" : plant,
            Quantity = quantity,
            Unit = string.IsNullOrWhiteSpace(unit) ? "EA" : unit,
            BatchNumber = batchNumber,
            VendorCode = vendorCode
        };

        var result = await _service.ProcessFrameAsync(request, HttpContext.RequestAborted);

        return Ok(new
        {
            passed = result.Passed,
            grade = result.Grade,
            impurityPct = result.ImpurityPct,
            defectType = result.DefectType,
            severity = result.Severity,
            confidence = result.Confidence,
            materialDocumentNumber = result.MaterialDocumentNumber,
            inspectionLotNumber = result.InspectionLotNumber,
            nonConformanceId = result.NonConformanceId,
            errors = result.Errors
        });
    }

    [HttpGet("results")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Results([FromQuery] string? materialCode, [FromQuery] int page = 1)
    {
        var rows = await _service.GetRecentResultsAsync(_tenant.TenantId, materialCode, page, 20);
        return Ok(new { data = rows, page = page <= 0 ? 1 : page, tenantId = _tenant.TenantId });
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
}
