using Microsoft.AspNetCore.SignalR;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Hubs;

namespace YuktiraERP.Infrastructure.Services;

public class LiveUpdateService : ILiveUpdateService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public LiveUpdateService(IHubContext<NotificationHub> hubContext) => _hubContext = hubContext;

    public async Task SendWorkflowUpdateAsync(Guid tenantId, Guid instanceId, string status, string message)
    {
        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("WorkflowUpdate", new
        {
            instanceId,
            status,
            message,
            timestamp = DateTime.UtcNow
        });
        await _hubContext.Clients.Group($"workflow_{instanceId}").SendAsync("WorkflowUpdate", new
        {
            instanceId,
            status,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendMrpProgressAsync(Guid tenantId, int percentage, string message)
    {
        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("MrpProgress", new
        {
            percentage,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendDashboardRefreshAsync(Guid tenantId)
    {
        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("DashboardRefresh", new
        {
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendNotificationAsync(Guid tenantId, string userId, string title, string message)
    {
        await _hubContext.Clients.Group($"tenant_{tenantId}").SendAsync("Notification", new
        {
            userId,
            title,
            message,
            timestamp = DateTime.UtcNow
        });
    }
}
