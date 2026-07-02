using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "PowerUserOrAbove")]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly ITenantContext _tenant;

    public WorkflowController(IWorkflowEngine workflowEngine, ITenantContext tenant)
    {
        _workflowEngine = workflowEngine;
        _tenant = tenant;
    }

    [HttpPost("{workflowId}/start")]
    public async Task<IActionResult> StartWorkflow(Guid workflowId, [FromBody] StartWorkflowRequest request)
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var instanceId = await _workflowEngine.StartWorkflowAsync(workflowId, _tenant.TenantId, request.EntityName, request.EntityId, userId, request.Variables);
        return Ok(new { instanceId, tenantId = _tenant.TenantId });
    }

    [HttpPost("instances/{instanceId}/process/{nodeId}")]
    public async Task<IActionResult> ProcessNode(Guid instanceId, Guid nodeId, [FromBody] Dictionary<string, object>? data = null)
    {
        await _workflowEngine.ProcessNodeAsync(instanceId, nodeId, data);
        return Ok(new { message = "Node processed", tenantId = _tenant.TenantId });
    }

    [HttpGet("{workflowId}/nodes")]
    public async Task<IActionResult> GetNodes(Guid workflowId)
    {
        var nodes = await _workflowEngine.GetWorkflowNodesAsync(workflowId);
        return Ok(new { data = nodes, tenantId = _tenant.TenantId });
    }

    [HttpPost("instances/{instanceId}/complete")]
    public async Task<IActionResult> CompleteWorkflow(Guid instanceId)
    {
        await _workflowEngine.CompleteWorkflowAsync(instanceId);
        return Ok(new { message = "Workflow completed", tenantId = _tenant.TenantId });
    }

    [HttpPost("instances/{instanceId}/terminate")]
    public async Task<IActionResult> TerminateWorkflow(Guid instanceId)
    {
        await _workflowEngine.TerminateWorkflowAsync(instanceId);
        return Ok(new { message = "Workflow terminated", tenantId = _tenant.TenantId });
    }

    [HttpPost("{workflowId}/validate")]
    public async Task<IActionResult> ValidateWorkflow(Guid workflowId)
    {
        var result = await _workflowEngine.ValidateWorkflowDefinitionAsync(workflowId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("{workflowId}/simulate")]
    public async Task<IActionResult> SimulateWorkflow(Guid workflowId, [FromBody] SimulateWorkflowRequest request)
    {
        var result = await _workflowEngine.SimulateWorkflowAsync(workflowId, request.Variables ?? new());
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("condition/evaluate")]
    public async Task<IActionResult> EvaluateCondition([FromBody] EvaluateConditionRequest request)
    {
        var result = await _workflowEngine.EvaluateConditionAsync(request.Expression, request.Variables ?? new());
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("{instanceId}/timer")]
    public async Task<IActionResult> ScheduleTimer(Guid instanceId, [FromBody] ScheduleTimerRequest request)
    {
        var delay = TimeSpan.FromMinutes(request.DelayMinutes);
        await _workflowEngine.ScheduleTimerAsync(instanceId, delay, request.Action);
        return Ok(new { message = "Timer scheduled", tenantId = _tenant.TenantId });
    }
}

public class StartWorkflowRequest
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Dictionary<string, object>? Variables { get; set; }
}

public class SimulateWorkflowRequest
{
    public Dictionary<string, object>? Variables { get; set; }
}

public class EvaluateConditionRequest
{
    public string Expression { get; set; } = "";
    public Dictionary<string, object>? Variables { get; set; }
}

public class ScheduleTimerRequest
{
    public double DelayMinutes { get; set; }
    public string Action { get; set; } = "";
}
