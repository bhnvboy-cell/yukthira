using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/mdg")]
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

    [HttpPost("requests/submit")]
    public async Task<IActionResult> SubmitRequest([FromBody] MdgSubmitRequest request)
    {
        var result = await _mdgService.SubmitChangeRequestAsync(request, _tenant.TenantId);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRequest(Guid id, [FromBody] MdgApprovalRequest approval)
    {
        var result = await _mdgService.ApproveChangeRequestAsync(id, approval, _tenant.TenantId);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id, [FromBody] MdgRejectionRequest rejection)
    {
        var result = await _mdgService.RejectChangeRequestAsync(id, rejection, _tenant.TenantId);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("requests/{id:guid}/activate")]
    public async Task<IActionResult> ActivateRequest(Guid id)
    {
        var result = await _mdgService.ActivateChangeRequestAsync(id, _tenant.TenantId);
        return Ok(new { success = true, data = result });
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
        var result = await _mdgService.GetChangeRequestAsync(id, _tenant.TenantId);
        if (result == null) return NotFound();
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }
}
