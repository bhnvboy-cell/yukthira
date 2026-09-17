using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/security")]
[Authorize]
public class SecurityImportController : ControllerBase
{
    private readonly ISecurityImportService _service;
    private readonly ILogger<SecurityImportController> _logger;

    public SecurityImportController(ISecurityImportService service, ILogger<SecurityImportController> logger)
    {
        _service = service; _logger = logger;
    }

    private Guid GetTenantId() => Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
    private Guid GetUserId() => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    [HttpPost("import/master-roles")]
    public async Task<IActionResult> ImportMasterRoles([FromBody] List<SecurityImportRowDto> rows)
    {
        var result = await _service.ImportMasterRolesAsync(rows, GetTenantId(), GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("import/composite-roles")]
    public async Task<IActionResult> ImportCompositeRoles([FromBody] List<CompositeRoleImportRowDto> rows)
    {
        var result = await _service.ImportCompositeRolesAsync(rows, GetTenantId(), GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("import/full-matrix")]
    public async Task<IActionResult> ImportFullMatrix([FromBody] RoleMatrixImportRequest request)
    {
        var result = await _service.ImportFullRoleMatrixAsync(request, GetTenantId(), GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("roles/master")]
    public async Task<IActionResult> GetMasterRoles([FromQuery] string? module = null)
    {
        var roles = await _service.GetMasterRolesAsync(GetTenantId(), module);
        return Ok(roles);
    }

    [HttpGet("roles/composite")]
    public async Task<IActionResult> GetCompositeRoles([FromQuery] string? module = null)
    {
        var roles = await _service.GetCompositeRolesAsync(GetTenantId(), module);
        return Ok(roles);
    }

    [HttpGet("roles/derived")]
    public async Task<IActionResult> GetDerivedRoles([FromQuery] string compositeRoleId)
    {
        var roles = await _service.GetDerivedRolesAsync(compositeRoleId);
        return Ok(roles);
    }

    [HttpGet("roles/hierarchy")]
    public async Task<IActionResult> GetRoleHierarchy()
    {
        var hierarchy = await _service.GetRoleHierarchyAsync(GetTenantId());
        return Ok(hierarchy);
    }

    [HttpPost("roles/assign")]
    public async Task<IActionResult> AssignRole([FromBody] UserRoleAssignRequest request)
    {
        var result = await _service.AssignCompositeRoleToUserAsync(request, GetTenantId(), GetUserId().ToString());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("roles/user/{userId}")]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        var assignments = await _service.GetUserRoleAssignmentsAsync(userId, GetTenantId());
        return Ok(assignments);
    }

    [HttpGet("roles/{roleId}/tcodes")]
    public async Task<IActionResult> GetRoleTcodes(string roleId, [FromQuery] string roleType = "Master")
    {
        var tcodes = await _service.GetRoleTCodePermissionsAsync(roleId, roleType, GetTenantId());
        return Ok(tcodes);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        return Ok(new
        {
            RoleCount = await _service.GetRoleCountAsync(GetTenantId()),
            TCodeCount = await _service.GetTCodeCountAsync(GetTenantId())
        });
    }
}
