using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/vision")]
[Authorize]
public class VisionInspectionController : ControllerBase
{
    private readonly IQualityVisionInspectionEngine _visionEngine;
    private readonly IZqmNonConformanceService _ncService;
    private readonly ITenantContext _tenant;

    public VisionInspectionController(
        IQualityVisionInspectionEngine visionEngine,
        IZqmNonConformanceService ncService,
        ITenantContext tenant)
    {
        _visionEngine = visionEngine;
        _ncService = ncService;
        _tenant = tenant;
    }

    [HttpPost("inspect")]
    public async Task<IActionResult> InspectImage([FromBody] VisionInspectionRequest request)
    {
        request.TenantId = _tenant.TenantId;
        var result = await _visionEngine.InspectImageAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("inspect/upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> InspectImageUpload(
        [FromForm] IFormFile image,
        [FromForm] string materialCode,
        [FromForm] string plant,
        [FromForm] string inspectionLotNumber)
    {
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);

        var request = new VisionInspectionRequest
        {
            TenantId = _tenant.TenantId,
            MaterialCode = materialCode,
            Plant = plant,
            InspectionLotNumber = inspectionLotNumber,
            ImageData = ms.ToArray(),
            ImageContentType = image.ContentType
        };

        var result = await _visionEngine.InspectImageAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("batch")]
    public async Task<IActionResult> InspectBatch([FromBody] List<VisionInspectionRequest> images)
    {
        foreach (var img in images)
            img.TenantId = _tenant.TenantId;

        var result = await _visionEngine.InspectBatchAsync(images, _tenant.TenantId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("batch/upload")]
    [RequestSizeLimit(200 * 1024 * 1024)]
    public async Task<IActionResult> InspectBatchUpload(
        [FromForm] List<IFormFile> images,
        [FromForm] string materialCode,
        [FromForm] string plant,
        [FromForm] string inspectionLotNumber)
    {
        var requests = new List<VisionInspectionRequest>();

        foreach (var image in images)
        {
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            requests.Add(new VisionInspectionRequest
            {
                TenantId = _tenant.TenantId,
                MaterialCode = materialCode,
                Plant = plant,
                InspectionLotNumber = inspectionLotNumber,
                ImageData = ms.ToArray(),
                ImageContentType = image.ContentType
            });
        }

        var result = await _visionEngine.InspectBatchAsync(requests, _tenant.TenantId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("train")]
    public async Task<IActionResult> TrainModel([FromBody] ModelTrainingRequest request)
    {
        request.TenantId = _tenant.TenantId;
        var result = await _visionEngine.TrainModelAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateModel([FromBody] ModelEvaluationRequest request)
    {
        request.TenantId = _tenant.TenantId;
        var result = await _visionEngine.EvaluateModelAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("defect-types")]
    public IActionResult GetDefectTypes()
    {
        var types = Enum.GetValues(typeof(VisionDefectType))
            .Cast<VisionDefectType>()
            .Select(v => new { Value = (int)v, Name = v.ToString() });
        return Ok(types);
    }

    [HttpGet("severity-levels")]
    public IActionResult GetSeverityLevels()
    {
        var levels = Enum.GetValues(typeof(VisionSeverity))
            .Cast<VisionSeverity>()
            .Select(v => new { Value = (int)v, Name = v.ToString() });
        return Ok(levels);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetInspectionHistory([FromQuery] int limit = 50)
    {
        var ncs = await _ncService.GetNonConformancesAsync(_tenant.TenantId);
        var limited = ncs.Take(limit).ToList();
        return Ok(limited);
    }
}
