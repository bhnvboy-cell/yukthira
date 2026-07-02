using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class PluginService : IPluginService
{
    private readonly YuktiraDbContext _db;

    public PluginService(YuktiraDbContext db) => _db = db;

    public async Task<List<PluginDto>> GetAllPluginsAsync()
    {
        return await _db.Plugins.Select(p => new PluginDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Version = p.Version,
            Description = p.Description,
            AssemblyPath = p.AssemblyPath,
            IsEnabledGlobal = p.IsEnabledGlobal,
            IsEnabledForTenant = p.IsEnabledForTenant
        }).ToListAsync();
    }

    public async Task<List<PluginDto>> GetTenantPluginsAsync(Guid tenantId)
    {
        return await _db.Plugins
            .Where(p => p.IsEnabledForTenant)
            .Select(p => new PluginDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Version = p.Version,
                Description = p.Description,
                AssemblyPath = p.AssemblyPath,
                IsEnabledGlobal = p.IsEnabledGlobal,
                IsEnabledForTenant = p.IsEnabledForTenant
            }).ToListAsync();
    }

    public async Task<PluginDto?> GetPluginByIdAsync(Guid pluginId)
    {
        var p = await _db.Plugins.FindAsync(pluginId);
        if (p == null) return null;
        return new PluginDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Version = p.Version,
            Description = p.Description,
            AssemblyPath = p.AssemblyPath,
            IsEnabledGlobal = p.IsEnabledGlobal,
            IsEnabledForTenant = p.IsEnabledForTenant
        };
    }

    public async Task<bool> InstallPluginAsync(string code)
    {
        if (await _db.Plugins.AnyAsync(p => p.Code == code))
            return false;

        _db.Plugins.Add(new PluginEntity
        {
            Code = code,
            Name = code,
            Version = "1.0.0",
            IsEnabledGlobal = true
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UninstallPluginAsync(Guid pluginId)
    {
        var plugin = await _db.Plugins.FindAsync(pluginId);
        if (plugin == null) return false;

        var settings = await _db.PluginSettings.Where(s => s.PluginId == pluginId).ToListAsync();
        _db.PluginSettings.RemoveRange(settings);
        _db.Plugins.Remove(plugin);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EnableForTenantAsync(Guid pluginId, Guid tenantId)
    {
        var p = await _db.Plugins.FindAsync(pluginId);
        if (p == null) return false;
        var existing = await _db.PluginTenantPermissions
            .FirstOrDefaultAsync(pt => pt.PluginId == pluginId && pt.TenantId == tenantId);
        if (existing != null)
        {
            existing.IsEnabled = true;
        }
        else
        {
            _db.PluginTenantPermissions.Add(new PluginTenantPermissionEntity
            {
                PluginId = pluginId,
                TenantId = tenantId,
                IsEnabled = true
            });
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DisableForTenantAsync(Guid pluginId, Guid tenantId)
    {
        var p = await _db.Plugins.FindAsync(pluginId);
        if (p == null) return false;
        var existing = await _db.PluginTenantPermissions
            .FirstOrDefaultAsync(pt => pt.PluginId == pluginId && pt.TenantId == tenantId);
        if (existing != null)
        {
            existing.IsEnabled = false;
            await _db.SaveChangesAsync();
        }
        return true;
    }

    public async Task<string?> GetSettingAsync(Guid pluginId, Guid tenantId, string key)
    {
        return (await _db.PluginSettings
            .Where(s => s.PluginId == pluginId && s.TenantId == tenantId && s.Key == key)
            .FirstOrDefaultAsync())?.Value;
    }

    public async Task SetSettingAsync(Guid pluginId, Guid tenantId, string key, string value)
    {
        var existing = await _db.PluginSettings
            .Where(s => s.PluginId == pluginId && s.TenantId == tenantId && s.Key == key)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.Value = value;
        }
        else
        {
            _db.PluginSettings.Add(new PluginSettingEntity
            {
                PluginId = pluginId,
                TenantId = tenantId,
                Key = key,
                Value = value
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync(Guid pluginId, Guid tenantId)
    {
        return await _db.PluginSettings
            .Where(s => s.PluginId == pluginId && s.TenantId == tenantId)
            .ToDictionaryAsync(s => s.Key, s => s.Value);
    }
}