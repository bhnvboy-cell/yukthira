namespace YuktiraERP.Core.Interfaces;

public class DashboardWidgetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string WidgetType { get; set; } = string.Empty;
    public string? ConfigJson { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsVisible { get; set; }
}

public interface IDashboardService
{
    Task<List<DashboardWidgetDto>> GetUserDashboardAsync(Guid userId, Guid tenantId);
    Task<bool> SaveWidgetLayoutAsync(Guid userId, List<DashboardWidgetDto> widgets);
    Task<DashboardWidgetDto?> GetWidgetDataAsync(Guid widgetId, Guid tenantId);
    Task<List<DashboardWidgetDto>> GetAvailableWidgetsAsync(Guid tenantId, string roleCode);
}
