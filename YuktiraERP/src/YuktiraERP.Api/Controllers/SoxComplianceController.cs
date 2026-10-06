using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/sox")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class SoxComplianceController : ControllerBase
{
    private readonly ISoxComplianceService _sox;
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public SoxComplianceController(ISoxComplianceService sox, YuktiraDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _sox = sox;
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var tenantId = _tenant.TenantId;
        var duties = await _db.SoxDuties.CountAsync(d => d.TenantId == tenantId);
        var activeDuties = await _db.SoxDuties.CountAsync(d => d.TenantId == tenantId && d.IsActive);
        var assignments = await _db.SoxAssignments.CountAsync(a => a.TenantId == tenantId && a.IsActive);
        var openViolations = await _db.SoxViolations.CountAsync(v => v.TenantId == tenantId && v.Status == "Open");
        var openHigh = await _db.SoxViolations.CountAsync(v => v.TenantId == tenantId && v.Status == "Open" && v.Severity == "High");
        var resolved = await _db.SoxViolations.CountAsync(v => v.TenantId == tenantId && v.Status != "Open");
        return Ok(new { duties, activeDuties, assignments, openViolations, openHigh, resolved, tenantId });
    }

    [HttpGet("duties")]
    public async Task<IActionResult> GetDuties([FromQuery] bool includeInactive = false)
    {
        return Ok(await _sox.GetDutiesAsync(_tenant.TenantId, includeInactive));
    }

    [HttpPost("duties")]
    public async Task<IActionResult> CreateDuty([FromBody] SoxDutySaveRequest request)
    {
        var actor = GetActorName() ?? "unknown";
        var dto = await _sox.CreateDutyAsync(_tenant.TenantId, request, actor);
        if (dto is null) return BadRequest(new { error = "Duty code is required and must be unique" });

        await AuditAsync(ActionType.Create, "SoxDuty", dto.Id.ToString(), $"Duty {dto.DutyCode} created", null, dto.DutyCode);
        await ChainAsync("SoxDuty", dto.Id, "Create", $"Duty {dto.DutyCode} created");
        return Ok(dto);
    }

    [HttpPut("duties/{id:guid}")]
    public async Task<IActionResult> UpdateDuty(Guid id, [FromBody] SoxDutySaveRequest request)
    {
        var actor = GetActorName() ?? "unknown";
        var dto = await _sox.UpdateDutyAsync(_tenant.TenantId, id, request, actor);
        if (dto is null) return NotFound(new { error = "Duty not found" });

        await AuditAsync(ActionType.Update, "SoxDuty", dto.Id.ToString(), $"Duty {dto.DutyCode} updated", null, dto.DutyCode);
        await ChainAsync("SoxDuty", dto.Id, "Update", $"Duty {dto.DutyCode} updated");
        return Ok(dto);
    }

    [HttpDelete("duties/{id:guid}")]
    public async Task<IActionResult> DeleteDuty(Guid id)
    {
        var success = await _sox.DeleteDutyAsync(_tenant.TenantId, id);
        if (!success) return NotFound(new { error = "Duty not found" });

        await AuditAsync(ActionType.Delete, "SoxDuty", id.ToString(), $"Duty {id} deleted");
        await ChainAsync("SoxDuty", id, "Delete", $"Duty {id} deleted");
        return Ok(new { success });
    }

    [HttpGet("assignments")]
    public async Task<IActionResult> GetAssignments([FromQuery] string? userId, [FromQuery] bool activeOnly = true)
    {
        return Ok(await _sox.GetAssignmentsAsync(_tenant.TenantId, userId, activeOnly));
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> AssignDuty([FromBody] SoxAssignRequest request)
    {
        var actor = GetActorName();
        if (string.IsNullOrWhiteSpace(request.AssignedBy)) request.AssignedBy = actor ?? "unknown";

        var result = await _sox.AssignDutyToUserAsync(_tenant.TenantId, request);
        if (!result.Success) return BadRequest(new { error = result.Message });

        await AuditAsync(ActionType.Create, "SoxAssignment", result.AssignmentId.ToString(), $"Duty {request.DutyCode} assigned to user {request.UserId} by {request.AssignedBy}", null, request.DutyCode);
        await ChainAsync("SoxAssignment", result.AssignmentId, "Create", $"Duty {request.DutyCode} assigned to user {request.UserId}");
        return Ok(result);
    }

    [HttpDelete("assignments/{id:guid}")]
    public async Task<IActionResult> DeactivateAssignment(Guid id, [FromQuery] string? actor)
    {
        var result = await _sox.DeactivateAssignmentAsync(_tenant.TenantId, id, actor ?? "unknown");
        if (!result.Success) return NotFound(new { error = result.Message });

        await AuditAsync(ActionType.Delete, "SoxAssignment", id.ToString(), $"Assignment {id} deactivated by {actor ?? "unknown"}");
        await ChainAsync("SoxAssignment", id, "Deactivate", $"Assignment {id} deactivated");
        return Ok(new { success = true, message = result.Message });
    }

    [HttpGet("effective-duties/{userId}")]
    public async Task<IActionResult> GetEffectiveDuties(string userId)
    {
        return Ok(await _sox.GetEffectiveDutiesAsync(_tenant.TenantId, userId));
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan()
    {
        var actor = GetActorName() ?? "system";
        var result = await _sox.RunSoDScanAsync(_tenant.TenantId, actor);

        await AuditAsync(ActionType.ApiCall, "SoDScan", _tenant.TenantId.ToString(), $"SoD scan executed by {actor}: {result.NewViolations} new violations");
        return Ok(result);
    }

    [HttpGet("violations")]
    public async Task<IActionResult> GetViolations([FromQuery] string status = "Open", [FromQuery] string? severity = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var request = new ViolationQueryRequest
        {
            Status = status ?? "",
            Severity = severity ?? "",
            Page = Math.Max(1, page),
            PageSize = Math.Max(1, pageSize)
        };
        var result = await _sox.GetViolationsAsync(request);
        return Ok(new { violations = result.Violations, totalCount = result.TotalCount, page = result.Page, pageSize = result.PageSize, tenantId = _tenant.TenantId });
    }

    [HttpPost("violations/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveViolation(Guid id, [FromBody] ResolveViolationBody request)
    {
        var resolution = request.Resolution?.Trim() ?? "";
        if (resolution.Length == 0) return BadRequest(new { error = "Resolution notes are required" });

        var actorId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var aid) ? aid : Guid.Empty;
        var result = await _sox.ResolveViolationAsync(new ResolveViolationRequest
        {
            ViolationId = id,
            Resolution = resolution,
            RootCause = request.RootCause?.Trim() ?? "",
            ResolvedByUserId = actorId
        });
        if (result.Success)
            await ChainAsync("SoxDutyViolation", id, "Resolve", $"Violation {id} resolved");
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("violations/export")]
    public async Task<IActionResult> ExportViolations([FromQuery] string? status, [FromQuery] string? severity)
    {
        var stream = new MemoryStream();
        await _sox.ExportViolationsToCsvAsync(_tenant.TenantId, stream, status, severity);
        stream.Position = 0;
        return File(stream, "text/csv", $"sod_violations_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("audit-chain")]
    public async Task<IActionResult> GetAuditChain([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? entityType, [FromQuery] string? entityId)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        if (fromDate.TimeOfDay == TimeSpan.Zero)
            fromDate = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
        else
            fromDate = fromDate.ToUniversalTime();

        var toDate = to ?? DateTime.UtcNow;
        if (toDate.TimeOfDay == TimeSpan.Zero)
            toDate = DateTime.SpecifyKind(toDate.Date, DateTimeKind.Utc).AddDays(1).AddTicks(-1);
        else
            toDate = toDate.ToUniversalTime();

        var request = new AuditChainVerifyRequest
        {
            EntityType = entityType ?? "",
            EntityId = Guid.TryParse(entityId, out var eid) ? eid : Guid.Empty,
            FromDate = fromDate,
            ToDate = toDate
        };
        return Ok(await _sox.VerifyAuditChainAsync(request));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.AdminUsers.AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.UserName)
            .Select(u => new { id = u.Id, userId = u.UserId, userName = u.UserName, role = u.Role })
            .ToListAsync();
        return Ok(new { users, tenantId = _tenant.TenantId });
    }

    private async Task AuditAsync(ActionType actionType, string entityName, string entityId, string details, string? oldValue = null, string? newValue = null)
    {
        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = _tenant.TenantId,
            ModuleName = "Security",
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            Details = details
        });
    }

    private async Task ChainAsync(string entityType, Guid entityId, string action, string details)
    {
        await _sox.LogAuditTrailAsync(new AuditTrailLogRequest
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = GetUserId(),
            UserName = GetActorName() ?? "system",
            Details = details
        });
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    private string? GetActorName() => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
}

public class ResolveViolationBody
{
    public string? Resolution { get; set; }
    public string? RootCause { get; set; }
}
