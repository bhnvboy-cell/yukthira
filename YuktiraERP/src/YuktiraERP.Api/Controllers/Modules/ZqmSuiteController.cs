using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/zqm")]
[Authorize]
public class ZqmSuiteController : ControllerBase
{
    private readonly IZqmAutoLotGeneratorService _lotGenerator;
    private readonly IZqmResultsWorkbenchService _resultsWorkbench;
    private readonly IZqmUsageDecisionEngineService _udEngine;
    private readonly IZqmUdReversalEngineService _udReversal;
    private readonly IZqmHandlingUnitService _huService;
    private readonly IZqmQaWorklistService _worklist;
    private readonly IZqmNonConformanceService _ncService;
    private readonly IZqmLabCalculatorService _labCalc;
    private readonly IZqmCoaGeneratorService _coaGenerator;
    private readonly IZqmPipelineDiagnosticService _pipelineDiag;

    public ZqmSuiteController(
        IZqmAutoLotGeneratorService lotGenerator,
        IZqmResultsWorkbenchService resultsWorkbench,
        IZqmUsageDecisionEngineService udEngine,
        IZqmUdReversalEngineService udReversal,
        IZqmHandlingUnitService huService,
        IZqmQaWorklistService worklist,
        IZqmNonConformanceService ncService,
        IZqmLabCalculatorService labCalc,
        IZqmCoaGeneratorService coaGenerator,
        IZqmPipelineDiagnosticService pipelineDiag)
    {
        _lotGenerator = lotGenerator;
        _resultsWorkbench = resultsWorkbench;
        _udEngine = udEngine;
        _udReversal = udReversal;
        _huService = huService;
        _worklist = worklist;
        _ncService = ncService;
        _labCalc = labCalc;
        _coaGenerator = coaGenerator;
        _pipelineDiag = pipelineDiag;
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("TenantId");
        return claim != null && Guid.TryParse(claim.Value, out var tid) ? tid : Guid.Empty;
    }

    [HttpPost("lot/generate")]
    public async Task<IActionResult> GenerateInspectionLot([FromBody] InspectionLotAutoGenRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _lotGenerator.GenerateInspectionLotAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("lot/check-qm/{materialCode}/{plant}")]
    public async Task<IActionResult> IsQmEnabled(string materialCode, string plant)
    {
        var enabled = await _lotGenerator.IsQmEnabledMaterialAsync(materialCode, plant, GetTenantId());
        return Ok(new { materialCode, plant, qmEnabled = enabled });
    }

    [HttpGet("workbench/{lotNumber}")]
    public async Task<IActionResult> GetWorkbenchLot(string lotNumber)
    {
        var lot = await _resultsWorkbench.GetLotForRecordingAsync(lotNumber, GetTenantId());
        return lot != null ? Ok(lot) : NotFound();
    }

    [HttpPost("workbench/record")]
    public async Task<IActionResult> RecordResult([FromBody] ResultRecordingRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _resultsWorkbench.RecordCharacteristicResultAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("workbench/results/{lotNumber}")]
    public async Task<IActionResult> GetRecordedResults(string lotNumber)
    {
        var results = await _resultsWorkbench.GetRecordedResultsAsync(lotNumber);
        return Ok(results);
    }

    [HttpPost("workbench/complete/{lotNumber}")]
    public async Task<IActionResult> CompleteResults(string lotNumber, [FromQuery] string userId = "API")
    {
        var ok = await _resultsWorkbench.CompleteResultsRecordingAsync(lotNumber, userId);
        return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    [HttpPost("usage-decision")]
    public async Task<IActionResult> PostUsageDecision([FromBody] UsageDecisionEngineRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _udEngine.PostUsageDecisionAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("usage-decision/validate/{lotNumber}")]
    public async Task<IActionResult> ValidateUdCompleteness(string lotNumber)
    {
        var complete = await _udEngine.ValidateUdCompletenessAsync(lotNumber, GetTenantId());
        return Ok(new { lotNumber, udComplete = complete });
    }

    [HttpPost("ud-reversal")]
    public async Task<IActionResult> ReverseUd([FromBody] UdReversalEngineRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _udReversal.ReverseUdAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("handling-unit")]
    public async Task<IActionResult> CreateHandlingUnit([FromBody] HandlingUnitCreateRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _huService.CreateHandlingUnitAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("handling-unit/release/{huId}")]
    public async Task<IActionResult> ReleaseHandlingUnit(Guid huId, [FromQuery] string userId = "API")
    {
        var result = await _huService.ReleaseHandlingUnitAsync(huId, userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("handling-unit/lot/{lotNumber}")]
    public async Task<IActionResult> GetHandlingUnitsByLot(string lotNumber)
    {
        var hus = await _huService.GetHandlingUnitsByLotAsync(lotNumber, GetTenantId());
        return Ok(hus);
    }

    [HttpGet("handling-unit/queue/{warehouseNumber}")]
    public async Task<IActionResult> GetPackagingQueue(string warehouseNumber)
    {
        var hus = await _huService.GetPackagingQueueAsync(warehouseNumber, GetTenantId());
        return Ok(hus);
    }

    [HttpPost("worklist")]
    public async Task<IActionResult> GetWorklist([FromBody] QaWorklistFilter filter)
    {
        var results = await _worklist.GetWorklistAsync(filter, GetTenantId());
        return Ok(results);
    }

    [HttpPost("non-conformance")]
    public async Task<IActionResult> CreateNonConformance([FromBody] NonConformanceCreateRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _ncService.CreateNonConformanceAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("capa")]
    public async Task<IActionResult> CreateCapa([FromBody] CapaCreateRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _ncService.CreateCapaAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("non-conformance/{ncId}/containment")]
    public async Task<IActionResult> ActivateContainment(Guid ncId, [FromBody] ContainmentRequest request)
    {
        var ok = await _ncService.ActivateContainmentAsync(ncId, request.Action, request.Expiry, request.UserId);
        return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    [HttpPost("capa/{cpId}/complete")]
    public async Task<IActionResult> CompleteCapa(Guid cpId, [FromQuery] string userId = "API", [FromQuery] string notes = "")
    {
        var ok = await _ncService.CompleteCapaAsync(cpId, userId, notes);
        return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    [HttpPost("capa/{cpId}/verify")]
    public async Task<IActionResult> VerifyCapa(Guid cpId, [FromBody] CapaVerifyRequest request)
    {
        var ok = await _ncService.VerifyCapaAsync(cpId, request.UserId, request.Effective, request.Notes);
        return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
    }

    [HttpGet("non-conformances")]
    public async Task<IActionResult> GetNonConformances([FromQuery] string? status = null)
    {
        var ncs = await _ncService.GetNonConformancesAsync(GetTenantId(), status);
        return Ok(ncs);
    }

    [HttpGet("capas")]
    public async Task<IActionResult> GetCapas([FromQuery] string? status = null)
    {
        var capas = await _ncService.GetCapasAsync(GetTenantId(), status);
        return Ok(capas);
    }

    [HttpPost("lab/calculate")]
    public async Task<IActionResult> CalculateLab([FromBody] LabCalculationRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _labCalc.CalculateAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("lab/results/{lotNumber}")]
    public async Task<IActionResult> GetLabResults(string lotNumber)
    {
        var results = await _labCalc.GetAnalysisByLotAsync(lotNumber, GetTenantId());
        return Ok(results);
    }

    [HttpPost("coa/generate")]
    public async Task<IActionResult> GenerateCoa([FromBody] CoaGenerationRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _coaGenerator.GenerateCoaAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("coa/logs")]
    public async Task<IActionResult> GetCoaLogs([FromQuery] string? materialCode = null)
    {
        var logs = await _coaGenerator.GetCoaLogsAsync(GetTenantId(), materialCode);
        return Ok(logs);
    }

    [HttpPost("pipeline/trace")]
    public async Task<IActionResult> TracePipeline([FromBody] ZqmPipelineTraceRequest request)
    {
        request.TenantId = GetTenantId();
        var result = await _pipelineDiag.TraceFullPipelineAsync(request);
        return Ok(result);
    }

    [HttpGet("pipeline/validate-inspection/{lotNumber}")]
    public async Task<IActionResult> ValidateInspectionLot(string lotNumber)
    {
        var result = await _pipelineDiag.ValidateInspectionLotStepAsync(lotNumber, GetTenantId());
        return Ok(result);
    }

    [HttpGet("pipeline/validate-results/{lotNumber}")]
    public async Task<IActionResult> ValidateResults(string lotNumber)
    {
        var result = await _pipelineDiag.ValidateResultsStepAsync(lotNumber, GetTenantId());
        return Ok(result);
    }

    [HttpGet("pipeline/validate-ud/{lotNumber}")]
    public async Task<IActionResult> ValidateUsageDecision(string lotNumber)
    {
        var result = await _pipelineDiag.ValidateUsageDecisionStepAsync(lotNumber, GetTenantId());
        return Ok(result);
    }

    [HttpGet("pipeline/validate-release/{lotNumber}")]
    public async Task<IActionResult> ValidateQualityRelease(string lotNumber)
    {
        var result = await _pipelineDiag.ValidateQualityReleaseStepAsync(lotNumber, GetTenantId());
        return Ok(result);
    }

    [HttpGet("pipeline/validate-coa/{lotNumber}")]
    public async Task<IActionResult> ValidateCoa(string lotNumber)
    {
        var result = await _pipelineDiag.ValidateCoaStepAsync(lotNumber, GetTenantId());
        return Ok(result);
    }
}

public class ContainmentRequest
{
    public string Action { get; set; } = "";
    public DateTime? Expiry { get; set; }
    public string UserId { get; set; } = "";
}

public class CapaVerifyRequest
{
    public string UserId { get; set; } = "";
    public bool Effective { get; set; }
    public string Notes { get; set; } = "";
}
