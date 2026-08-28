using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Api.GraphQL;

/// <summary>
/// Real-time Dashboard Hub - pushes live KPI updates to connected clients.
/// </summary>
[Authorize]
public class DashboardHub : Hub
{
    private readonly YuktiraDbContext _db;

    public DashboardHub(YuktiraDbContext db) => _db = db;

    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard_{tenantId}");

        // Send initial dashboard data
        await SendDashboardUpdate();
        await base.OnConnectedAsync();
    }

    public async Task SubscribeDashboard()
    {
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard_{tenantId}");
    }

    public async Task SubscribeMaterial(string materialCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"material_{materialCode}");
    }

    public async Task SubscribeOrder(string orderType, string orderNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderType}_{orderNumber}");
    }

    public async Task SubscribeProduction(string orderNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"production_{orderNumber}");
    }

    public async Task RequestDashboardUpdate()
    {
        await SendDashboardUpdate();
    }

    public async Task RequestStockAlerts()
    {
        var stockItems = await _db.StockItems.AsNoTracking().ToListAsync();
        var alerts = stockItems
            .Where(s => s.Quantity <= s.MinStock || s.Quantity >= s.MaxStock)
            .Select(s => new
            {
                materialName = s.MaterialName,
                currentStock = s.Quantity,
                minStock = s.MinStock,
                maxStock = s.MaxStock,
                alertLevel = s.Quantity <= s.MinStock ? "LOW" : "OVER"
            })
            .ToList();

        await Clients.Caller.SendAsync("StockAlerts", alerts);
    }

    public async Task RequestProductionStatus()
    {
        var orders = await _db.ProductionOrders.AsNoTracking().ToListAsync();
        var status = new
        {
            total = orders.Count,
            planned = orders.Count(o => o.Status == "PLANNED"),
            inProgress = orders.Count(o => o.Status == "IN_PROGRESS"),
            completed = orders.Count(o => o.Status == "COMPLETED"),
            teco = orders.Count(o => o.Status == "TECO")
        };

        await Clients.Caller.SendAsync("ProductionStatus", status);
    }

    public async Task RequestQualityStatus()
    {
        var lots = await _db.InspectionLots.AsNoTracking().ToListAsync();
        var status = new
        {
            total = lots.Count,
            pending = lots.Count(l => l.Status == "Pending"),
            passed = lots.Sum(l => l.Passed),
            failed = lots.Sum(l => l.Failed),
            passRate = lots.Sum(l => l.Inspected) > 0
                ? Math.Round((decimal)lots.Sum(l => l.Passed) / lots.Sum(l => l.Inspected) * 100, 1)
                : 0
        };

        await Clients.Caller.SendAsync("QualityStatus", status);
    }

    private async Task SendDashboardUpdate()
    {
        var materials = await _db.MaterialMasters.AsNoTracking().CountAsync();
        var stockItems = await _db.StockItems.AsNoTracking().ToListAsync();
        var salesOrders = await _db.SalesOrders.AsNoTracking().ToListAsync();
        var prodOrders = await _db.ProductionOrders.AsNoTracking().ToListAsync();
        var qmLots = await _db.InspectionLots.AsNoTracking().ToListAsync();
        var journalEntries = await _db.UniversalJournals.AsNoTracking().ToListAsync();
        var movements = await _db.StockMovements.AsNoTracking().CountAsync();

        var dashboard = new
        {
            timestamp = DateTime.UtcNow,
            materials = materials,
            stockValue = stockItems.Sum(s => s.Value),
            lowStockCount = stockItems.Count(s => s.Quantity <= s.MinStock),
            totalRevenue = salesOrders.Sum(o => o.Amount),
            openOrders = salesOrders.Count(o => o.Status == "Pending"),
            productionActive = prodOrders.Count(o => o.Status == "IN_PROGRESS"),
            productionCompleted = prodOrders.Count(o => o.Status == "COMPLETED"),
            qmPassRate = qmLots.Sum(l => l.Inspected) > 0
                ? Math.Round((decimal)qmLots.Sum(l => l.Passed) / qmLots.Sum(l => l.Inspected) * 100, 1)
                : 0,
            journalEntries = journalEntries.Count,
            stockMovements = movements,
            totalDebits = journalEntries.Sum(j => j.DebitAmount),
            totalCredits = journalEntries.Sum(j => j.CreditAmount)
        };

        await Clients.Caller.SendAsync("DashboardUpdate", dashboard);
    }
}

/// <summary>
/// Service to push real-time notifications from background services.
/// </summary>
public interface IDashboardNotificationService
{
    Task NotifyStockChange(string materialName, decimal oldQty, decimal newQty);
    Task NotifyOrderUpdate(string orderType, string orderNumber, string status);
    Task NotifyProductionUpdate(string orderNumber, string status, decimal progress);
    Task NotifyQualityAlert(string lotNumber, string result);
    Task NotifyViolationDetected(string userId, string violationType, string severity);
    Task NotifyAnomalyDetected(string anomalyType, string entityName, decimal deviation);
}

public class DashboardNotificationService : IDashboardNotificationService
{
    private readonly IHubContext<DashboardHub> _hubContext;

    public DashboardNotificationService(IHubContext<DashboardHub> hubContext)
        => _hubContext = hubContext;

    public async Task NotifyStockChange(string materialName, decimal oldQty, decimal newQty)
    {
        await _hubContext.Clients.All.SendAsync("StockChange", new
        {
            materialName,
            oldQuantity = oldQty,
            newQuantity = newQty,
            change = newQty - oldQty,
            timestamp = DateTime.UtcNow
        });

        // Alert if low stock
        if (newQty <= 0)
        {
            await _hubContext.Clients.All.SendAsync("CriticalAlert", new
            {
                type = "STOCK_OUT",
                materialName,
                message = $"Material {materialName} is out of stock!",
                severity = "Critical",
                timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task NotifyOrderUpdate(string orderType, string orderNumber, string status)
    {
        await _hubContext.Clients.All.SendAsync("OrderUpdate", new
        {
            orderType,
            orderNumber,
            status,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyProductionUpdate(string orderNumber, string status, decimal progress)
    {
        await _hubContext.Clients.All.SendAsync("ProductionUpdate", new
        {
            orderNumber,
            status,
            progress,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyQualityAlert(string lotNumber, string result)
    {
        await _hubContext.Clients.All.SendAsync("QualityAlert", new
        {
            lotNumber,
            result,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyViolationDetected(string userId, string violationType, string severity)
    {
        await _hubContext.Clients.All.SendAsync("SoxViolation", new
        {
            userId,
            violationType,
            severity,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyAnomalyDetected(string anomalyType, string entityName, decimal deviation)
    {
        await _hubContext.Clients.All.SendAsync("AnomalyDetected", new
        {
            anomalyType,
            entityName,
            deviation,
            timestamp = DateTime.UtcNow
        });
    }
}
