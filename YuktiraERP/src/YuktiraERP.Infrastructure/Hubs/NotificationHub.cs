using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace YuktiraERP.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("TenantId")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
        await base.OnConnectedAsync();
    }

    public async Task SubscribeWorkflow(Guid workflowInstanceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow_{workflowInstanceId}");
    }

    public async Task UnsubscribeWorkflow(Guid workflowInstanceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workflow_{workflowInstanceId}");
    }
}
