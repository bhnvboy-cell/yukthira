using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOrAbove")]
public class PluginsController : ControllerBase
{
    private readonly IPluginService _pluginService;
    private readonly PluginLoader _pluginLoader;

    public PluginsController(IPluginService pluginService, PluginLoader pluginLoader)
    {
        _pluginService = pluginService;
        _pluginLoader = pluginLoader;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _pluginService.GetAllPluginsAsync());

    [HttpGet("tenant")]
    public async Task<IActionResult> GetTenantPlugins()
    {
        var tenantId = Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
        return Ok(await _pluginService.GetTenantPluginsAsync(tenantId));
    }

    [HttpPost("{code}/install")]
    public async Task<IActionResult> Install(string code)
    {
        var result = await _pluginService.InstallPluginAsync(code);
        return Ok(new { success = result });
    }

    [HttpDelete("{pluginId}/uninstall")]
    public async Task<IActionResult> Uninstall(Guid pluginId)
    {
        var result = await _pluginService.UninstallPluginAsync(pluginId);
        return Ok(new { success = result });
    }

    [HttpPost("{pluginId}/enable")]
    public async Task<IActionResult> EnableForTenant(Guid pluginId)
    {
        var tenantId = Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
        var result = await _pluginService.EnableForTenantAsync(pluginId, tenantId);
        return Ok(new { success = result });
    }

    [HttpPost("{pluginId}/disable")]
    public async Task<IActionResult> DisableForTenant(Guid pluginId)
    {
        var tenantId = Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
        var result = await _pluginService.DisableForTenantAsync(pluginId, tenantId);
        return Ok(new { success = result });
    }

    [HttpGet("{pluginId}/settings")]
    public async Task<IActionResult> GetSettings(Guid pluginId)
    {
        var tenantId = Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
        var settings = await _pluginService.GetAllSettingsAsync(pluginId, tenantId);
        return Ok(settings);
    }

    [HttpPost("{pluginId}/settings")]
    public async Task<IActionResult> SetSettings(Guid pluginId, [FromBody] Dictionary<string, string> settings)
    {
        var tenantId = Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
        foreach (var kvp in settings)
        {
            await _pluginService.SetSettingAsync(pluginId, tenantId, kvp.Key, kvp.Value);
        }
        return Ok(new { success = true });
    }

    [HttpGet("{pluginId}/permissions")]
    public async Task<IActionResult> GetPermissions(Guid pluginId)
    {
        var pluginDto = await _pluginService.GetPluginByIdAsync(pluginId);
        if (pluginDto == null) return NotFound();

        var plugin = _pluginLoader.GetPlugin(pluginDto.Code);
        if (plugin is IPluginPermissionProvider provider)
            return Ok(provider.GetPermissions());

        return Ok(Array.Empty<PluginPermission>());
    }

    [HttpPost("{pluginId}/reload")]
    public async Task<IActionResult> Reload(Guid pluginId)
    {
        var pluginDto = await _pluginService.GetPluginByIdAsync(pluginId);
        if (pluginDto == null) return NotFound();

        var result = _pluginLoader.ReloadPlugin(pluginDto.Code, pluginDto.AssemblyPath);
        if (result == null)
            return BadRequest(new { success = false, message = "Reload failed" });

        if (result is IPluginHotReload hotReload)
            await hotReload.OnAfterReloadAsync();

        return Ok(new { success = true });
    }

    [HttpGet("{pluginId}/status")]
    public async Task<IActionResult> GetStatus(Guid pluginId)
    {
        var pluginDto = await _pluginService.GetPluginByIdAsync(pluginId);
        if (pluginDto == null) return NotFound();

        var plugin = _pluginLoader.GetPlugin(pluginDto.Code);
        var isLoaded = plugin != null;
        var canHotReload = plugin is IPluginHotReload hr && hr.CanHotReload;

        return Ok(new
        {
            pluginId,
            pluginDto.Code,
            pluginDto.Name,
            pluginDto.Version,
            isLoaded,
            canHotReload,
            maxMemoryBytes = _pluginLoader.MaxMemoryPerPlugin,
            maxExecutionMs = _pluginLoader.MaxExecutionMsPerPlugin,
            isSandboxed = plugin is IPluginSandboxed
        });
    }
}
