using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly YuktiraDbContext _db;

    public DashboardService(YuktiraDbContext db) => _db = db;

    public async Task<List<DashboardWidgetDto>> GetUserDashboardAsync(Guid userId, Guid tenantId)
    {
        var dashboards = await _db.Dashboards
            .Where(d => d.TenantId == tenantId && d.Status == "Active")
            .ToListAsync();

        return dashboards.Select(d => new DashboardWidgetDto
        {
            Id = d.Id,
            Name = d.Name,
            Code = d.DashboardId,
            WidgetType = "CUSTOM",
            ConfigJson = d.ConfigJson,
            PositionX = 0,
            PositionY = 0,
            Width = 4,
            Height = 2,
            IsVisible = true
        }).ToList();
    }

    public async Task<bool> SaveWidgetLayoutAsync(Guid userId, List<DashboardWidgetDto> widgets)
    {
        foreach (var w in widgets)
        {
            var existing = await _db.Dashboards.FindAsync(w.Id);
            if (existing != null)
            {
                existing.ConfigJson = w.ConfigJson ?? "{}";
                existing.Status = w.IsVisible ? "Active" : "Inactive";
            }
            else
            {
                _db.Dashboards.Add(new DashboardEntity
                {
                    Id = w.Id,
                    DashboardId = w.Code,
                    Name = w.Name,
                    Category = w.WidgetType,
                    ConfigJson = w.ConfigJson ?? "{}",
                    Status = w.IsVisible ? "Active" : "Inactive"
                });
            }
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DashboardWidgetDto?> GetWidgetDataAsync(Guid widgetId, Guid tenantId)
    {
        var entity = await _db.Dashboards.FindAsync(widgetId);
        if (entity == null) return null;
        return new DashboardWidgetDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.DashboardId,
            WidgetType = entity.Category,
            ConfigJson = entity.ConfigJson,
            PositionX = 0,
            PositionY = 0,
            Width = 4,
            Height = 2,
            IsVisible = entity.Status == "Active"
        };
    }

    public Task<List<DashboardWidgetDto>> GetAvailableWidgetsAsync(Guid tenantId, string roleCode)
    {
        var systemWidgets = new List<DashboardWidgetDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Open Purchase Orders", Code = "OPEN_PO", WidgetType = "KPI", Width = 2, Height = 1, IsVisible = true },
            new() { Id = Guid.NewGuid(), Name = "Pending Approvals", Code = "PENDING_APPROVALS", WidgetType = "LIST", Width = 4, Height = 3, IsVisible = true },
            new() { Id = Guid.NewGuid(), Name = "Monthly Revenue", Code = "MONTHLY_REVENUE", WidgetType = "CHART", Width = 4, Height = 2, IsVisible = true },
            new() { Id = Guid.NewGuid(), Name = "Stock Overview", Code = "STOCK_OVERVIEW", WidgetType = "KPI", Width = 2, Height = 1, IsVisible = true },
            new() { Id = Guid.NewGuid(), Name = "Recent Notifications", Code = "RECENT_NOTIFICATIONS", WidgetType = "LIST", Width = 4, Height = 3, IsVisible = true },
            new() { Id = Guid.NewGuid(), Name = "My Tasks", Code = "MY_TASKS", WidgetType = "LIST", Width = 4, Height = 2, IsVisible = true }
        };
        return Task.FromResult(systemWidgets);
    }
}
