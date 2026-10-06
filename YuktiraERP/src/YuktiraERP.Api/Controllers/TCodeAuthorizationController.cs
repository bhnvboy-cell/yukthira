using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/security/tcode-authz")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class TCodeAuthorizationController : ControllerBase
{
    private static readonly string[] AvailableRoles = ["SUPER_USER", "ADMIN", "POWER_USER", "NORMAL_USER", "READ_ONLY"];
    private static readonly string[] ActionTypes = ["CREATE", "CHANGE", "DELETE", "DISPLAY", "APPROVE", "RECEIVE", "PAYMENT", "POST", "ADMIN"];
    private static readonly string[] Enforcements = ["Enforced", "DisplayOnly"];

    private readonly ITransactionCodeService _service;
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public TCodeAuthorizationController(ITransactionCodeService service, YuktiraDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? module, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var allCodes = await _db.TransactionCodes.AsNoTracking().ToListAsync();
        var modules = allCodes.Select(c => c.Module).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().OrderBy(m => m, StringComparer.Ordinal).ToList();

        var codes = allCodes;
        if (!string.IsNullOrWhiteSpace(module)) codes = codes.Where(c => c.Module == module).ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search;
            codes = codes.Where(c =>
                c.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        codes = codes.OrderBy(c => c.Module).ThenBy(c => c.SortOrder).ThenBy(c => c.Code).ToList();

        var tenantId = _tenant.TenantId;
        var checks = await _db.TCodeAuthChecks.AsNoTracking().Where(c => c.TenantId == tenantId).ToListAsync();
        var checkCounts = checks.GroupBy(c => c.TCode).ToDictionary(g => g.Key, g => g.Count());
        var denies = await _db.TransactionPermissions.AsNoTracking().Where(p => !p.CanAccess).ToListAsync();
        var denyCounts = denies.GroupBy(p => p.TransactionCodeId).ToDictionary(g => g.Key, g => g.Count());

        var total = codes.Count;
        var pageNumber = Math.Max(1, page);
        var size = Math.Max(1, Math.Min(pageSize, 500));
        var items = codes.Skip((pageNumber - 1) * size).Take(size).Select(c => new
        {
            id = c.Id,
            code = c.Code,
            name = c.Name,
            module = c.Module,
            group = c.GroupName,
            requiredRole = c.RequiredRole,
            status = c.Status,
            checkCount = checkCounts.TryGetValue(c.Code, out var cc) ? cc : 0,
            denyCount = denyCounts.TryGetValue(c.Id, out var dc) ? dc : 0
        }).ToList();

        return Ok(new { items, total, modules, tenantId });
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var upper = code.Trim().ToUpperInvariant();
        var entity = await _db.TransactionCodes.AsNoTracking().FirstOrDefaultAsync(c => c.Code.ToUpper() == upper);
        if (entity is null) return NotFound(new { error = "Transaction code not found" });

        var tenantId = _tenant.TenantId;
        var checks = await _db.TCodeAuthChecks.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.TCode == upper)
            .OrderBy(c => c.CheckCode)
            .Select(c => new
            {
                id = c.Id,
                tcode = c.TCode,
                checkCode = c.CheckCode,
                actionType = c.ActionType,
                requiredRole = c.RequiredRole,
                enforcement = c.Enforcement,
                description = c.Description,
                isActive = c.IsActive
            })
            .ToListAsync();

        var permissions = await _service.GetPermissionsAsync(entity.Id);
        return Ok(new
        {
            id = entity.Id,
            code = entity.Code,
            name = entity.Name,
            description = entity.Description,
            module = entity.Module,
            requiredRole = entity.RequiredRole,
            status = entity.Status,
            route = entity.Route,
            icon = entity.Icon,
            checks,
            permissions,
            availableRoles = AvailableRoles,
            actionTypes = ActionTypes,
            tenantId
        });
    }

    [HttpPut("{code}/required-role")]
    public async Task<IActionResult> UpdateRequiredRole(string code, [FromBody] RequiredRoleRequest request)
    {
        var value = request.RequiredRole?.Trim() ?? "";
        if (!AvailableRoles.Any(r => string.Equals(r, value, StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { error = "requiredRole must be one of SUPER_USER, ADMIN, POWER_USER, NORMAL_USER, READ_ONLY" });

        value = value.ToUpperInvariant();
        var upper = code.Trim().ToUpperInvariant();
        var entity = await _db.TransactionCodes.FirstOrDefaultAsync(c => c.Code.ToUpper() == upper);
        if (entity is null) return NotFound(new { error = "Transaction code not found" });

        var previous = entity.RequiredRole;
        entity.RequiredRole = value;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = _tenant.TenantId,
            ModuleName = "Security",
            ActionType = ActionType.Update,
            EntityName = "TransactionCode",
            EntityId = entity.Id.ToString(),
            OldValue = previous,
            NewValue = value,
            Details = $"Transaction code {entity.Code} required role changed from {previous} to {value}"
        });

        return Ok(new { id = entity.Id, code = entity.Code, requiredRole = value, previousRole = previous });
    }

    [HttpPost("checks")]
    public async Task<IActionResult> CreateCheck([FromBody] TCodeAuthCheckRequest request)
    {
        var validation = ValidateCheck(request, "");
        if (validation.Error is not null) return BadRequest(new { error = validation.Error });

        var tenantId = _tenant.TenantId;
        if (await _db.TCodeAuthChecks.AnyAsync(c => c.TenantId == tenantId && c.TCode == validation.TCode && c.CheckCode == validation.CheckCode))
            return BadRequest(new { error = "Check already exists for this transaction code" });

        var entity = new TCodeAuthCheckEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TCode = validation.TCode,
            CheckCode = validation.CheckCode,
            ActionType = validation.ActionType,
            RequiredRole = validation.RequiredRole,
            Enforcement = validation.Enforcement,
            Description = validation.Description,
            IsActive = request.IsActive
        };
        _db.TCodeAuthChecks.Add(entity);
        await _db.SaveChangesAsync();

        await AuditAsync(ActionType.Create, "TCodeAuthCheck", entity.Id.ToString(), $"Check {entity.TCode}/{entity.CheckCode} created", null, entity.RequiredRole);

        return Ok(CheckDto(entity));
    }

    [HttpPut("checks/{id:guid}")]
    public async Task<IActionResult> UpdateCheck(Guid id, [FromBody] TCodeAuthCheckRequest request)
    {
        var tenantId = _tenant.TenantId;
        var entity = await _db.TCodeAuthChecks.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (entity is null) return NotFound(new { error = "Check not found" });

        var validation = ValidateCheck(request, entity.TCode);
        if (validation.Error is not null) return BadRequest(new { error = validation.Error });

        if (await _db.TCodeAuthChecks.AnyAsync(c => c.Id != id && c.TenantId == tenantId && c.TCode == validation.TCode && c.CheckCode == validation.CheckCode))
            return BadRequest(new { error = "Check already exists for this transaction code" });

        var previousRole = entity.RequiredRole;
        entity.TCode = validation.TCode;
        entity.CheckCode = validation.CheckCode;
        entity.ActionType = validation.ActionType;
        entity.RequiredRole = validation.RequiredRole;
        entity.Enforcement = validation.Enforcement;
        entity.Description = validation.Description;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await AuditAsync(ActionType.Update, "TCodeAuthCheck", entity.Id.ToString(), $"Check {entity.TCode}/{entity.CheckCode} updated", previousRole, entity.RequiredRole);

        return Ok(CheckDto(entity));
    }

    [HttpDelete("checks/{id:guid}")]
    public async Task<IActionResult> DeleteCheck(Guid id)
    {
        var tenantId = _tenant.TenantId;
        var entity = await _db.TCodeAuthChecks.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
        if (entity is null) return NotFound(new { error = "Check not found" });

        _db.TCodeAuthChecks.Remove(entity);
        await _db.SaveChangesAsync();

        await AuditAsync(ActionType.Delete, "TCodeAuthCheck", entity.Id.ToString(), $"Check {entity.TCode}/{entity.CheckCode} deleted", entity.CheckCode, null);

        return Ok(new { success = true });
    }

    [HttpPost("{code}/test-access")]
    public async Task<IActionResult> TestAccess(string code, [FromBody] TCodeAccessTestRequest? request = null)
    {
        var existing = await _service.GetByCodeAsync(code);
        if (existing is null) return NotFound(new { error = "Transaction code not found" });

        Guid? userId = Guid.TryParse(request?.UserId, out var uid) ? uid : null;
        var role = string.IsNullOrWhiteSpace(request?.Role) ? null : request!.Role;
        var result = await _service.CheckAccessDetailedAsync(code, userId, role);
        return Ok(new
        {
            code = code.ToUpperInvariant(),
            effectiveRole = result.EffectiveRole,
            requiredRole = result.RequiredRole,
            allowed = result.Allowed,
            ruleSource = result.RuleSource,
            reason = result.Reason
        });
    }

    private static CheckValidation ValidateCheck(TCodeAuthCheckRequest request, string fallbackTCode)
    {
        var validation = new CheckValidation();
        var tcode = string.IsNullOrWhiteSpace(request.TCode) ? fallbackTCode.Trim() : request.TCode.Trim().ToUpperInvariant();
        if (tcode.Length == 0)
        {
            validation.Error = "tcode is required";
            return validation;
        }
        var checkCode = request.CheckCode?.Trim() ?? "";
        if (checkCode.Length == 0)
        {
            validation.Error = "checkCode is required";
            return validation;
        }
        var requiredRole = request.RequiredRole?.Trim() ?? "";
        if (!AvailableRoles.Any(r => string.Equals(r, requiredRole, StringComparison.OrdinalIgnoreCase)))
        {
            validation.Error = "requiredRole must be one of SUPER_USER, ADMIN, POWER_USER, NORMAL_USER, READ_ONLY";
            return validation;
        }
        var actionType = ActionTypes.FirstOrDefault(a => string.Equals(a, request.ActionType, StringComparison.OrdinalIgnoreCase));
        if (actionType is null)
        {
            validation.Error = "actionType must be one of CREATE, CHANGE, DELETE, DISPLAY, APPROVE, RECEIVE, PAYMENT, POST, ADMIN";
            return validation;
        }
        var enforcement = Enforcements.FirstOrDefault(e => string.Equals(e, request.Enforcement, StringComparison.OrdinalIgnoreCase));
        if (enforcement is null)
        {
            validation.Error = "enforcement must be one of Enforced, DisplayOnly";
            return validation;
        }

        validation.TCode = tcode;
        validation.CheckCode = checkCode;
        validation.ActionType = actionType;
        validation.RequiredRole = requiredRole.ToUpperInvariant();
        validation.Enforcement = enforcement;
        validation.Description = request.Description?.Trim() ?? "";
        return validation;
    }

    private static object CheckDto(TCodeAuthCheckEntity entity) => new
    {
        id = entity.Id,
        tcode = entity.TCode,
        checkCode = entity.CheckCode,
        actionType = entity.ActionType,
        requiredRole = entity.RequiredRole,
        enforcement = entity.Enforcement,
        description = entity.Description,
        isActive = entity.IsActive
    };

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

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
}

public class RequiredRoleRequest
{
    public string? RequiredRole { get; set; }
}

public class TCodeAuthCheckRequest
{
    public string? TCode { get; set; }
    public string? CheckCode { get; set; }
    public string? ActionType { get; set; }
    public string? RequiredRole { get; set; }
    public string? Enforcement { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TCodeAccessTestRequest
{
    public string? Role { get; set; }
    public string? UserId { get; set; }
}

public class CheckValidation
{
    public string? Error { get; set; }
    public string TCode { get; set; } = "";
    public string CheckCode { get; set; } = "";
    public string ActionType { get; set; } = "";
    public string RequiredRole { get; set; } = "";
    public string Enforcement { get; set; } = "";
    public string Description { get; set; } = "";
}
