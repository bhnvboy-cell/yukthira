using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService) => _approvalService = approvalService;

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();
        var pending = await _approvalService.GetPendingApprovalsAsync(tenantId, userId);
        return Ok(pending);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var approval = await _approvalService.GetApprovalByIdAsync(id);
        if (approval == null) return NotFound();
        return Ok(approval);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovalActionRequest request)
    {
        var userId = GetUserId();
        var result = await _approvalService.ApproveAsync(id, userId, request.Comments);
        return Ok(new { success = result });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ApprovalActionRequest request)
    {
        var userId = GetUserId();
        var result = await _approvalService.RejectAsync(id, userId, request.Comments ?? "Rejected");
        return Ok(new { success = result });
    }

    [HttpPost("{id}/escalate")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> Escalate(Guid id)
    {
        var result = await _approvalService.EscalateAsync(id);
        return Ok(new { success = result });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApprovalRequest request)
    {
        var tenantId = GetTenantId();
        var userId = GetUserId();
        var id = await _approvalService.CreateApprovalRequestAsync(tenantId, request.Module, request.DocumentType, request.DocumentId, request.DocumentNumber, request.Amount, userId);
        return Ok(new { id });
    }

    private Guid GetUserId() =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
}

public class ApprovalActionRequest { public string? Comments { get; set; } }
public class CreateApprovalRequest
{
    public string Module { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
