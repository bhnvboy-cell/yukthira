using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly ITransactionSequenceService _sequenceService;

    public WorkflowController(ITransactionSequenceService sequenceService)
    {
        _sequenceService = sequenceService;
    }

    [HttpGet("chains")]
    public async Task<IActionResult> GetChains()
    {
        var chains = await _sequenceService.GetWorkflowChainsAsync();
        return Ok(chains);
    }

    [HttpGet("chains/{chainId}")]
    public async Task<IActionResult> GetChain(string chainId)
    {
        var chain = await _sequenceService.GetChainByIdAsync(chainId);
        if (chain == null) return NotFound(new { error = $"Chain '{chainId}' not found" });
        return Ok(chain);
    }

    [HttpPost("chains/{chainId}/validate")]
    public async Task<IActionResult> ValidateStep(string chainId, [FromBody] ValidateStepRequest request)
    {
        var result = await _sequenceService.ValidateStepAsync(chainId, request.TCode, request.Context);
        return Ok(result);
    }

    [HttpPost("chains/{chainId}/execute")]
    public async Task<IActionResult> ExecuteStep(string chainId, [FromBody] ExecuteStepRequest request)
    {
        var userId = GetUserId();
        var result = await _sequenceService.ExecuteStepAsync(chainId, request.TCode, userId, request.Parameters);
        return Ok(result);
    }

    [HttpGet("chains/{chainId}/progress")]
    public async Task<IActionResult> GetProgress(string chainId, [FromQuery] string? instanceId = null)
    {
        var steps = await _sequenceService.GetChainProgressAsync(chainId, instanceId);
        return Ok(steps);
    }

    [HttpGet("instances")]
    public async Task<IActionResult> GetInstances([FromQuery] string? chainId = null)
    {
        var instances = await _sequenceService.GetActiveInstancesAsync(chainId);
        return Ok(instances);
    }

    [HttpGet("instances/{instanceId}")]
    public async Task<IActionResult> GetInstance(string instanceId)
    {
        var instance = await _sequenceService.GetInstanceAsync(instanceId);
        if (instance == null) return NotFound(new { error = $"Instance '{instanceId}' not found" });
        return Ok(instance);
    }

    private Guid GetUserId()
    {
        var claim = System.Security.Claims.ClaimTypes.NameIdentifier;
        var val = User.FindFirst(claim)?.Value;
        return string.IsNullOrEmpty(val) ? Guid.Empty : Guid.Parse(val);
    }
}

public class ValidateStepRequest
{
    public string TCode { get; set; } = "";
    public Dictionary<string, object>? Context { get; set; }
}

public class ExecuteStepRequest
{
    public string TCode { get; set; } = "";
    public Dictionary<string, object>? Parameters { get; set; }
}
