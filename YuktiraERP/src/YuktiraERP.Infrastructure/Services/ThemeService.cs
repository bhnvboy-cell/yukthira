using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ThemeService : IThemeService
{
    private static readonly List<string> _themes = new() { "modern", "classic", "premium" };
    private readonly YuktiraDbContext _db;

    public ThemeService(YuktiraDbContext db) => _db = db;

    public Task<List<string>> GetAvailableThemesAsync() => Task.FromResult(_themes);

    public async Task<string> GetUserThemeAsync(Guid userId, Guid tenantId)
    {
        var setting = await _db.TenantSettings
            .Where(s => s.TenantCode == userId.ToString() && s.Name == "theme")
            .FirstOrDefaultAsync();
        if (setting != null) return setting.Status;

        var tenantSetting = await _db.TenantSettings
            .Where(s => s.TenantCode == tenantId.ToString() && s.Name == "default_theme")
            .FirstOrDefaultAsync();
        return tenantSetting?.Status ?? "modern";
    }

    public async Task SetUserThemeAsync(Guid userId, Guid tenantId, string theme)
    {
        var existing = await _db.TenantSettings
            .Where(s => s.TenantCode == userId.ToString() && s.Name == "theme")
            .FirstOrDefaultAsync();
        if (existing != null)
            existing.Status = theme;
        else
            _db.TenantSettings.Add(new TenantSettingEntity
            {
                TenantCode = userId.ToString(),
                Name = "theme",
                Subdomain = "user",
                Status = theme
            });
        await _db.SaveChangesAsync();
    }

    public async Task<string> GetTenantDefaultThemeAsync(Guid tenantId)
    {
        var setting = await _db.TenantSettings
            .Where(s => s.TenantCode == tenantId.ToString() && s.Name == "default_theme")
            .FirstOrDefaultAsync();
        return setting?.Status ?? "modern";
    }

    public async Task SetTenantDefaultThemeAsync(Guid tenantId, string theme)
    {
        var existing = await _db.TenantSettings
            .Where(s => s.TenantCode == tenantId.ToString() && s.Name == "default_theme")
            .FirstOrDefaultAsync();
        if (existing != null)
            existing.Status = theme;
        else
            _db.TenantSettings.Add(new TenantSettingEntity
            {
                TenantCode = tenantId.ToString(),
                Name = "default_theme",
                Subdomain = "tenant",
                Status = theme
            });
        await _db.SaveChangesAsync();
    }
}
