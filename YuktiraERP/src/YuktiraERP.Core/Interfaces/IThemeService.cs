namespace YuktiraERP.Core.Interfaces;

public interface IThemeService
{
    Task<List<string>> GetAvailableThemesAsync();
    Task<string> GetUserThemeAsync(Guid userId, Guid tenantId);
    Task SetUserThemeAsync(Guid userId, Guid tenantId, string theme);
    Task<string> GetTenantDefaultThemeAsync(Guid tenantId);
    Task SetTenantDefaultThemeAsync(Guid tenantId, string theme);
}
