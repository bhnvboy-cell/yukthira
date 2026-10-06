using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/security/matrix")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class SecurityMatrixController : ControllerBase
{
    private readonly ITransactionCodeService _service;
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public SecurityMatrixController(ITransactionCodeService service, YuktiraDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _service = service;
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    [HttpGet("roles-tcodes")]
    public async Task<IActionResult> GetRolesTcodes([FromQuery] string? by = "user", [FromQuery] string? module = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var mode = NormalizeMode(by);
        var matrix = await BuildMatrixAsync(mode, module, search);
        var pageNumber = Math.Max(1, page);
        var size = Math.Max(1, Math.Min(pageSize, 500));
        var rows = matrix.Rows.Skip((pageNumber - 1) * size).Take(size).ToList();
        return Ok(new { by = mode, modules = matrix.Modules, tcodeCount = matrix.TCodeCount, rows, totalCount = matrix.Rows.Count, tenantId = _tenant.TenantId });
    }

    [HttpGet("hierarchy")]
    public async Task<IActionResult> GetHierarchy()
    {
        var masterRoles = await _db.MasterRoles.AsNoTracking().OrderBy(r => r.RoleId).ToListAsync();
        var compositeRoles = await _db.CompositeRoles.AsNoTracking().OrderBy(r => r.CompositeRoleId).ToListAsync();
        var derivedRoles = await _db.DerivedRoles.AsNoTracking().OrderBy(r => r.DerivedRoleId).ToListAsync();
        var roleTcodeAssignments = await _db.RoleTCodeAssignments.AsNoTracking()
            .OrderBy(a => a.RoleType).ThenBy(a => a.RoleId).ThenBy(a => a.TransactionCode).ToListAsync();
        var userRoleAssignments = await _db.UserRoleAssignments.AsNoTracking()
            .OrderBy(a => a.UserId).ThenBy(a => a.CompositeRoleId).ToListAsync();
        var users = await _db.AdminUsers.AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new { id = u.Id, userId = u.UserId, userName = u.UserName, email = u.Email, role = u.Role, isActive = u.IsActive, lastLoginAt = u.LastLoginAt })
            .ToListAsync();
        return Ok(new { masterRoles, compositeRoles, derivedRoles, roleTcodeAssignments, userRoleAssignments, users, tenantId = _tenant.TenantId });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? by = "user", [FromQuery] string? module = null)
    {
        static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0) return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        var mode = NormalizeMode(by);
        var matrix = await BuildMatrixAsync(mode, module, null);
        var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true))
        {
            if (mode == "role")
            {
                await writer.WriteLineAsync("Role,UserCount,PermittedCount,DeniedCount,PermittedCodes");
                foreach (var row in matrix.Rows)
                {
                    await writer.WriteLineAsync(string.Join(",",
                        CsvEscape(row.Label),
                        Convert.ToString(row.Secondary) ?? "",
                        row.PermittedCount.ToString(),
                        row.DeniedCount.ToString(),
                        CsvEscape(string.Join(" ", row.Permitted))));
                }
            }
            else
            {
                await writer.WriteLineAsync("UserName,LoginId,Role,Active,PermittedCount,DeniedCount,PermittedCodes");
                foreach (var row in matrix.Rows)
                {
                    await writer.WriteLineAsync(string.Join(",",
                        CsvEscape(row.Label),
                        CsvEscape(Convert.ToString(row.Secondary)),
                        CsvEscape(row.Role),
                        row.Active ? "true" : "false",
                        row.PermittedCount.ToString(),
                        row.DeniedCount.ToString(),
                        CsvEscape(string.Join(" ", row.Permitted))));
                }
            }
            await writer.FlushAsync();
        }
        stream.Position = 0;
        return File(stream, "text/csv", $"access_matrix_{mode}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpPut("role-tcodes")]
    public async Task<IActionResult> SaveRoleTcodes([FromBody] RoleTcodeSaveRequest request)
    {
        var roleType = NormalizeRoleType(request.RoleType);
        if (roleType is null) return BadRequest(new { error = "roleType must be one of Master, Composite, Derived" });
        var roleId = request.RoleId?.Trim() ?? "";
        if (roleId.Length == 0) return BadRequest(new { error = "roleId is required" });

        var tenantId = _tenant.TenantId;
        var existing = await _db.RoleTCodeAssignments
            .Where(a => a.RoleId == roleId && a.RoleType == roleType && a.TenantId == tenantId)
            .ToListAsync();
        if (existing.Count > 0) _db.RoleTCodeAssignments.RemoveRange(existing);

        var added = 0;
        foreach (var code in request.Codes ?? Array.Empty<string>())
        {
            var value = code?.Trim().ToUpperInvariant() ?? "";
            if (value.Length == 0) continue;
            _db.RoleTCodeAssignments.Add(new RoleTCodeAssignmentEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RoleId = roleId,
                RoleType = roleType,
                TransactionCode = value,
                HasAccess = true,
                AppId = "",
                AppDescription = ""
            });
            added++;
        }
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = tenantId,
            ModuleName = "Security",
            ActionType = ActionType.Update,
            EntityName = "RoleTCodeAssignment",
            EntityId = roleId,
            NewValue = roleType,
            Details = $"Role {roleType} {roleId} transaction codes updated ({added} codes)"
        });

        return Ok(new { saved = added });
    }

    private async Task<MatrixResult> BuildMatrixAsync(string mode, string? module, string? search)
    {
        var codes = await _db.TransactionCodes.AsNoTracking().ToListAsync();
        var activeCodes = codes.Where(c => c.Status == "Active").ToList();
        var modules = activeCodes
            .Select(c => c.Module)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();
        var scopedActive = string.IsNullOrWhiteSpace(module)
            ? activeCodes
            : activeCodes.Where(c => c.Module == module).ToList();

        var users = await _db.AdminUsers.AsNoTracking().OrderBy(u => u.UserName).ToListAsync();
        var rows = new List<AccessMatrixRow>();

        if (mode == "role")
        {
            var roles = users
                .Select(u => u.Role)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .OrderBy(r => r, StringComparer.Ordinal)
                .ToList();
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(search) && !role.Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                var permitted = await _service.GetPermittedCodesAsync(null, role);
                var scopedPermitted = string.IsNullOrWhiteSpace(module)
                    ? permitted
                    : permitted.Where(c => c.Module == module).ToList();
                rows.Add(new AccessMatrixRow
                {
                    Key = role,
                    Label = role,
                    Secondary = users.Count(u => u.Role == role),
                    Role = role,
                    Active = true,
                    PermittedCount = scopedPermitted.Count,
                    DeniedCount = scopedActive.Count - scopedPermitted.Count,
                    Permitted = scopedPermitted.Select(c => c.Code).OrderBy(c => c, StringComparer.Ordinal).ToList()
                });
            }
        }
        else
        {
            foreach (var user in users)
            {
                if (!string.IsNullOrWhiteSpace(search)
                    && !user.UserName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    && !user.UserId.Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
                var permitted = await _service.GetPermittedCodesAsync(user.Id, user.Role);
                var scopedPermitted = string.IsNullOrWhiteSpace(module)
                    ? permitted
                    : permitted.Where(c => c.Module == module).ToList();
                rows.Add(new AccessMatrixRow
                {
                    Key = user.Id.ToString(),
                    Label = user.UserName,
                    Secondary = user.UserId,
                    Role = user.Role,
                    Active = user.IsActive,
                    PermittedCount = scopedPermitted.Count,
                    DeniedCount = scopedActive.Count - scopedPermitted.Count,
                    Permitted = scopedPermitted.Select(c => c.Code).OrderBy(c => c, StringComparer.Ordinal).ToList()
                });
            }
        }

        return new MatrixResult { Rows = rows, Modules = modules, TCodeCount = activeCodes.Count };
    }

    private static string NormalizeMode(string? by) =>
        string.Equals(by, "role", StringComparison.OrdinalIgnoreCase) ? "role" : "user";

    private static string? NormalizeRoleType(string? roleType)
    {
        var allowed = new[] { "Master", "Composite", "Derived" };
        if (string.IsNullOrWhiteSpace(roleType)) return null;
        foreach (var candidate in allowed)
        {
            if (string.Equals(candidate, roleType.Trim(), StringComparison.OrdinalIgnoreCase)) return candidate;
        }
        return null;
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
}

public class RoleTcodeSaveRequest
{
    public string? RoleType { get; set; }
    public string? RoleId { get; set; }
    public string[]? Codes { get; set; }
}

public class AccessMatrixRow
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public object Secondary { get; set; } = "";
    public string Role { get; set; } = "";
    public bool Active { get; set; }
    public int PermittedCount { get; set; }
    public int DeniedCount { get; set; }
    public List<string> Permitted { get; set; } = new();
}

public class MatrixResult
{
    public List<AccessMatrixRow> Rows { get; set; } = new();
    public List<string> Modules { get; set; } = new();
    public int TCodeCount { get; set; }
}
