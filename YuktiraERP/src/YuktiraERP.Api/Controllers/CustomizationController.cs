using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomizationController : ControllerBase
{
    private readonly ICustomizationService _customizationService;

    public CustomizationController(ICustomizationService customizationService) => _customizationService = customizationService;

    [HttpGet("{screenName}")]
    public async Task<IActionResult> GetScreenLayout(string screenName)
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();
        var layout = await _customizationService.GetScreenLayoutAsync(userId, tenantId, screenName);
        return Ok(layout);
    }

    [HttpPost("{screenName}")]
    public async Task<IActionResult> SaveLayout(string screenName, [FromBody] List<ColumnDefinitionDto> columns)
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();
        var result = await _customizationService.SaveUserLayoutAsync(userId, tenantId, screenName, columns);
        return Ok(new { success = result });
    }

    [HttpDelete("{screenName}/reset")]
    public async Task<IActionResult> ResetLayout(string screenName)
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();
        var result = await _customizationService.ResetLayoutAsync(userId, tenantId, screenName);
        return Ok(new { success = result });
    }

    [HttpPost("{screenName}/columns")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> AddColumn(string screenName, [FromBody] ColumnDefinitionDto column)
    {
        var tenantId = GetTenantId();
        var result = await _customizationService.AddColumnAsync(tenantId, screenName, column);
        return Ok(new { success = result });
    }

    [HttpDelete("{screenName}/columns/{columnName}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteColumn(string screenName, string columnName)
    {
        var tenantId = GetTenantId();
        var result = await _customizationService.DeleteColumnAsync(tenantId, screenName, columnName);
        return Ok(new { success = result });
    }

    private Guid GetUserId() =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
}
