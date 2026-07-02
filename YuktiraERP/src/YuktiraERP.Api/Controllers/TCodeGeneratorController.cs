using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/tcode-generator")]
[Authorize(Roles = "SUPER_USER,ADMIN")]
public class TCodeGeneratorController : ControllerBase
{
    private readonly ITCodeGeneratorService _service;
    private readonly ITenantContext _tenant;

    public TCodeGeneratorController(ITCodeGeneratorService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    [HttpGet("definitions")]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllDefinitionsAsync(_tenant.TenantId));

    [HttpGet("definitions/{code}")]
    public async Task<IActionResult> Get(string code)
    {
        var result = await _service.GetDefinitionAsync(_tenant.TenantId, code);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpPost("definitions")]
    public async Task<IActionResult> Create([FromBody] CreateTCodeRequest req)
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var result = await _service.CreateDefinitionAsync(_tenant.TenantId, req.Code, req.Name, req.Module, req.Description, userId);
        return Ok(result);
    }

    [HttpDelete("definitions/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeleteDefinitionAsync(_tenant.TenantId, id);
        return ok ? Ok() : NotFound();
    }

    [HttpGet("definitions/{tcodeId}/fields")]
    public async Task<IActionResult> GetFields(Guid tcodeId) => Ok(await _service.GetFieldsAsync(_tenant.TenantId, tcodeId));

    [HttpPost("definitions/{tcodeId}/fields")]
    public async Task<IActionResult> AddField(Guid tcodeId, [FromBody] TCodeFieldDto field)
    {
        var result = await _service.AddFieldAsync(_tenant.TenantId, tcodeId, field);
        return Ok(result);
    }

    [HttpDelete("fields/{id}")]
    public async Task<IActionResult> RemoveField(Guid id)
    {
        var ok = await _service.RemoveFieldAsync(_tenant.TenantId, id);
        return ok ? Ok() : NotFound();
    }

    [HttpGet("data/{tcodeId}")]
    public async Task<IActionResult> GetRecords(Guid tcodeId, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        => Ok(await _service.GetRecordsAsync(_tenant.TenantId, tcodeId, search, page, pageSize));

    [HttpPost("data/{tcodeId}")]
    public async Task<IActionResult> CreateRecord(Guid tcodeId, [FromBody] Dictionary<string, object> data)
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var result = await _service.CreateRecordAsync(_tenant.TenantId, tcodeId, data, userId);
        return Ok(result);
    }

    [HttpPut("data/{recordId}")]
    public async Task<IActionResult> UpdateRecord(Guid recordId, [FromBody] Dictionary<string, object> data)
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var result = await _service.UpdateRecordAsync(_tenant.TenantId, recordId, data, userId);
        return Ok(result);
    }

    [HttpDelete("data/{recordId}")]
    public async Task<IActionResult> DeleteRecord(Guid recordId)
    {
        var ok = await _service.DeleteRecordAsync(_tenant.TenantId, recordId);
        return ok ? Ok() : NotFound();
    }

    [HttpGet("data/record/{recordId}")]
    public async Task<IActionResult> GetRecord(Guid recordId)
    {
        var result = await _service.GetRecordAsync(_tenant.TenantId, recordId);
        return result != null ? Ok(result) : NotFound();
    }

    [HttpGet("workflow-definition")]
    public IActionResult GetWorkflowDefinition() => Ok(_service.GetWorkflowDefinition());

    [HttpGet("data/{recordId}/workflow")]
    public async Task<IActionResult> GetRecordWorkflow(Guid recordId)
    {
        var result = await _service.GetRecordWorkflowAsync(_tenant.TenantId, recordId);
        return Ok(result);
    }

    [HttpPost("data/{recordId}/workflow/advance")]
    public async Task<IActionResult> AdvanceWorkflow(Guid recordId, [FromBody] AdvanceWorkflowRequest req)
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var result = await _service.AdvanceWorkflowAsync(_tenant.TenantId, recordId, req.TargetNode, userId);
        return Ok(result);
    }

    [HttpPost("data/{recordId}/workflow/reject")]
    public async Task<IActionResult> RejectWorkflow(Guid recordId)
    {
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var result = await _service.RejectWorkflowAsync(_tenant.TenantId, recordId, userId);
        return Ok(result);
    }
}

public record CreateTCodeRequest(string Code, string Name, string Module, string Description);
public record AdvanceWorkflowRequest(string TargetNode);
