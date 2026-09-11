using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Hubs;

public class YuktiraNotificationHub : Hub
{
    private readonly ILogger<YuktiraNotificationHub> _logger;

    public YuktiraNotificationHub(ILogger<YuktiraNotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            _logger.LogInformation("Client connected to notification hub: Tenant={Tenant}, Connection={Connection}",
                tenantId, Context.ConnectionId);
        }

        var deviceId = Context.GetHttpContext()?.Request.Query["deviceId"].FirstOrDefault();
        if (!string.IsNullOrEmpty(deviceId) && !string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: Connection={Connection}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToMaterial(string materialCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"material_{materialCode}");
    }

    public async Task UnsubscribeFromMaterial(string materialCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"material_{materialCode}");
    }

    public async Task SubscribeToPlant(string plant)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"plant_{plant}");
    }

    public async Task SubscribeToDevice(string deviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
    }

    public async Task SubscribeWorkflow(Guid workflowInstanceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow_{workflowInstanceId}");
    }

    public async Task SubscribeInspectionLot(string lotNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"lot_{lotNumber}");
    }

    public async Task AcknowledgeAlert(Guid alertId)
    {
        _logger.LogInformation("Alert acknowledged: AlertId={AlertId}, Connection={Connection}",
            alertId, Context.ConnectionId);
        await Clients.Caller.SendAsync("AlertAcknowledged", alertId);
    }
}

public class MobileNotificationService : IMobileNotificationService
{
    private readonly IHubContext<YuktiraNotificationHub> _hubContext;
    private readonly ILogger<MobileNotificationService> _logger;

    public MobileNotificationService(
        IHubContext<YuktiraNotificationHub> hubContext,
        ILogger<MobileNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendLowStockAlertAsync(LowStockNotificationDto notification, Guid tenantId)
    {
        var alert = new
        {
            Type = MobileAlertType.LowStock.ToString(),
            Title = $"Low Stock: {notification.MaterialCode}",
            Message = $"{notification.MaterialCode} at {notification.Plant}/{notification.StorageLocation} " +
                      $"is below minimum. Current: {notification.CurrentQuantity} {notification.Unit}, " +
                      $"Minimum: {notification.MinimumQuantity} {notification.Unit}",
            Priority = notification.CurrentQuantity <= 0 ? "Critical" : "High",
            notification.MaterialCode,
            notification.Plant,
            notification.StorageLocation,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"tenant_{tenantId}")
            .SendAsync("ReceiveAlert", alert);

        _logger.LogWarning("Low stock alert sent: Material={Material}, Plant={Plant}, Qty={Qty}",
            notification.MaterialCode, notification.Plant, notification.CurrentQuantity);
    }

    public async Task SendProductionHoldAlertAsync(ProductionHoldNotificationDto notification, Guid tenantId)
    {
        var alert = new
        {
            Type = MobileAlertType.ProductionHold.ToString(),
            Title = $"Production Hold: {notification.ProductionOrderNumber}",
            Message = $"Production order {notification.ProductionOrderNumber} for {notification.MaterialCode} " +
                      $"at {notification.Plant} is on hold. Reason: {notification.Reason}",
            Priority = "High",
            notification.ProductionOrderNumber,
            notification.MaterialCode,
            notification.Plant,
            notification.HoldStatus,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"tenant_{tenantId}")
            .SendAsync("ReceiveAlert", alert);

        _logger.LogWarning("Production hold alert sent: Order={Order}, Material={Material}",
            notification.ProductionOrderNumber, notification.MaterialCode);
    }

    public async Task SendQualityNonConformanceAlertAsync(QualityAlertDto notification, Guid tenantId)
    {
        var priority = notification.Severity switch
        {
            "Critical" => "Critical",
            "Major" => "High",
            _ => "Medium"
        };

        var alert = new
        {
            Type = MobileAlertType.QualityNonConformance.ToString(),
            Title = $"Quality Alert: {notification.InspectionLotNumber}",
            Message = $"Non-conformance detected in lot {notification.InspectionLotNumber} " +
                      $"for {notification.MaterialCode} at {notification.Plant}. " +
                      $"Defect: {notification.DefectType}. Action: {notification.RecommendedAction}",
            Priority = priority,
            notification.InspectionLotNumber,
            notification.MaterialCode,
            notification.Plant,
            notification.Severity,
            notification.DefectType,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"tenant_{tenantId}")
            .SendAsync("ReceiveAlert", alert);

        _logger.LogWarning("Quality alert sent: Lot={Lot}, Severity={Severity}",
            notification.InspectionLotNumber, notification.Severity);
    }

    public async Task SendGenericAlertAsync(GenericAlertDto notification, Guid tenantId)
    {
        var alert = new
        {
            Type = notification.AlertType.ToString(),
            notification.Title,
            notification.Message,
            notification.Priority,
            notification.Data,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"tenant_{tenantId}")
            .SendAsync("ReceiveAlert", alert);

        _logger.LogInformation("Generic alert sent: Type={Type}, Title={Title}",
            notification.AlertType, notification.Title);
    }
}
