using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/mdg")]
[Authorize]
public class MdgController : ControllerBase
{
    private readonly IMdgService _mdgService;
    private readonly ITenantContext _tenant;

    public MdgController(IMdgService mdgService, ITenantContext tenant)
    {
        _mdgService = mdgService;
        _tenant = tenant;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string? status,
        [FromQuery] string? entityName,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mdgService.ListRequestsAsync(status, entityName, search, page, pageSize);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("requests/pending")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var result = await _mdgService.GetPendingRequestsAsync(_tenant.TenantId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("requests/{id:guid}")]
    public async Task<IActionResult> GetRequest(Guid id)
    {
        var result = await _mdgService.GetRequestAsync(id);
        if (result == null) return NotFound();
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("requests/{id:guid}/diff")]
    public async Task<IActionResult> GetRequestDiff(Guid id)
    {
        var result = await _mdgService.GetDiffAsync(id);
        if (result == null) return NotFound();
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("requests/{id:guid}/audit")]
    public async Task<IActionResult> GetRequestAudit(Guid id)
    {
        var result = await _mdgService.GetAuditTrailAsync(id);
        if (result == null) return NotFound();
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpPost("requests/draft")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> CreateDraft([FromBody] MdgCreateDraftRequest request)
    {
        if (request != null && string.IsNullOrWhiteSpace(request.RequestedBy))
            request.RequestedBy = CurrentUserName();

        try
        {
            var result = await _mdgService.CreateDraftAsync(request);
            return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: 400, detail: ex.Message);
        }
    }

    [HttpPost("requests/submit")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> SubmitRequest([FromBody] MdgSubmitRequest request)
    {
        if (request != null && string.IsNullOrWhiteSpace(request.RequestedBy))
            request.RequestedBy = CurrentUserName() ?? "";

        try
        {
            var result = await _mdgService.SubmitChangeRequestAsync(request, _tenant.TenantId);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: 400, detail: ex.Message);
        }
    }

    [HttpPost("requests/{id:guid}/submit")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> SubmitDraft(Guid id)
    {
        try
        {
            var result = await _mdgService.SubmitAsync(id);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: 400, detail: ex.Message);
        }
    }

    [HttpPost("requests/{id:guid}/approve")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> ApproveRequest(Guid id, [FromBody] MdgApprovalRequest approval)
    {
        if (approval != null && string.IsNullOrWhiteSpace(approval.ApprovedBy))
            approval.ApprovedBy = CurrentUserName() ?? "";

        try
        {
            var result = await _mdgService.ApproveChangeRequestAsync(id, approval, _tenant.TenantId);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: 400, detail: ex.Message);
        }
    }

    [HttpPost("requests/{id:guid}/reject")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> RejectRequest(Guid id, [FromBody] MdgRejectionRequest rejection)
    {
        if (rejection != null && string.IsNullOrWhiteSpace(rejection.RejectedBy))
            rejection.RejectedBy = CurrentUserName() ?? "";

        try
        {
            var result = await _mdgService.RejectChangeRequestAsync(id, rejection, _tenant.TenantId);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: 400, detail: ex.Message);
        }
    }

    [HttpPost("requests/{id:guid}/activate")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> ActivateRequest(Guid id)
    {
        try
        {
            var result = await _mdgService.ActivateChangeRequestAsync(id, _tenant.TenantId);
            return Ok(new { success = true, data = result });
        }
        catch (InvalidOperationException ex)
        {
            return Problem(statusCode: 400, detail: ex.Message);
        }
    }

    private string? CurrentUserName()
    {
        return User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
    }
}
