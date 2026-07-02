namespace YuktiraERP.Core.Interfaces;

public class PluginDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AssemblyPath { get; set; } = "";
    public bool IsEnabledGlobal { get; set; }
    public bool IsEnabledForTenant { get; set; }
}

public interface IPluginService
{
    Task<List<PluginDto>> GetAllPluginsAsync();
    Task<List<PluginDto>> GetTenantPluginsAsync(Guid tenantId);
    Task<bool> InstallPluginAsync(string code);
    Task<bool> UninstallPluginAsync(Guid pluginId);
    Task<bool> EnableForTenantAsync(Guid pluginId, Guid tenantId);
    Task<bool> DisableForTenantAsync(Guid pluginId, Guid tenantId);
    Task<string?> GetSettingAsync(Guid pluginId, Guid tenantId, string key);
    Task SetSettingAsync(Guid pluginId, Guid tenantId, string key, string value);
    Task<Dictionary<string, string>> GetAllSettingsAsync(Guid pluginId, Guid tenantId);
    Task<PluginDto?> GetPluginByIdAsync(Guid pluginId);
}
