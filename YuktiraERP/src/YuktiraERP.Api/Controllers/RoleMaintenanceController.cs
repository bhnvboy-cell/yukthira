using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/security/roles")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class RoleMaintenanceController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public RoleMaintenanceController(YuktiraDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenantId = _tenant.TenantId;
        var items = await _db.CompositeRoles.AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.CompositeRoleId)
            .ToListAsync();
        return Ok(new { items, tenantId = _tenant.TenantId });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CompositeRoleSaveRequest request)
    {
        var roleId = request.CompositeRoleId?.Trim() ?? "";
        var roleName = request.CompositeRoleName?.Trim() ?? "";
        if (roleId.Length == 0 || roleName.Length == 0)
            return BadRequest(new { error = "Composite role ID and name are required" });

        var tenantId = _tenant.TenantId;
        if (await _db.CompositeRoles.AnyAsync(r => r.TenantId == tenantId && r.CompositeRoleId == roleId))
            return BadRequest(new { error = "Role ID already exists" });

        var entity = new CompositeRoleEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompositeRoleId = roleId,
            CompositeRoleName = roleName,
            Module = request.Module?.Trim() ?? "",
            Description = request.Description?.Trim() ?? "",
            Status = "Active"
        };
        _db.CompositeRoles.Add(entity);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = tenantId,
            ModuleName = "Security",
            ActionType = ActionType.Create,
            EntityName = "CompositeRole",
            EntityId = entity.Id.ToString(),
            NewValue = roleId,
            Details = $"Composite role {roleId} created"
        });

        return Ok(ToDto(entity));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CompositeRoleSaveRequest request)
    {
        var tenantId = _tenant.TenantId;
        var entity = await _db.CompositeRoles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);
        if (entity is null) return NotFound(new { error = "Role not found" });

        var roleName = request.CompositeRoleName?.Trim() ?? "";
        if (roleName.Length == 0) roleName = entity.CompositeRoleName;

        var previous = entity.CompositeRoleName;
        entity.CompositeRoleName = roleName;
        entity.Module = request.Module?.Trim() ?? entity.Module;
        entity.Description = request.Description?.Trim() ?? entity.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = tenantId,
            ModuleName = "Security",
            ActionType = ActionType.Update,
            EntityName = "CompositeRole",
            EntityId = entity.Id.ToString(),
            OldValue = previous,
            NewValue = roleName,
            Details = $"Composite role {entity.CompositeRoleId} updated"
        });

        return Ok(ToDto(entity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tenantId = _tenant.TenantId;
        var entity = await _db.CompositeRoles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);
        if (entity is null) return NotFound(new { error = "Role not found" });

        entity.Status = "Inactive";
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = tenantId,
            ModuleName = "Security",
            ActionType = ActionType.Delete,
            EntityName = "CompositeRole",
            EntityId = entity.Id.ToString(),
            OldValue = entity.CompositeRoleId,
            Details = $"Composite role {entity.CompositeRoleId} deactivated"
        });

        return Ok(new { success = true });
    }

    private static object ToDto(CompositeRoleEntity entity) => new
    {
        id = entity.Id,
        compositeRoleId = entity.CompositeRoleId,
        compositeRoleName = entity.CompositeRoleName,
        module = entity.Module,
        description = entity.Description,
        status = entity.Status,
        createdAt = entity.CreatedAt,
        updatedAt = entity.UpdatedAt,
        tenantId = entity.TenantId
    };

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
}

public class CompositeRoleSaveRequest
{
    public string? CompositeRoleId { get; set; }
    public string? CompositeRoleName { get; set; }
    public string? Module { get; set; }
    public string? Description { get; set; }
}
