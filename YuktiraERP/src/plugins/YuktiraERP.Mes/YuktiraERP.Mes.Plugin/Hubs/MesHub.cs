using Microsoft.AspNetCore.SignalR;

namespace YuktiraERP.Mes.Plugin.Hubs;

/// <summary>
/// Live MES SignalR hub at /hubs/mes.
/// Groups by tenant:plant:line so terminals receive only their own stream (Section 13.5).
/// </summary>
public class MesHub : Hub
{
    public static string BuildGroupName(Guid tenantId, string plantCode, string lineCode)
        => $"tenant:{tenantId}:plant:{plantCode}:line:{lineCode}";

    public Task JoinLineGroup(Guid tenantId, string plantCode, string lineCode)
        => Groups.AddToGroupAsync(Context.ConnectionId, BuildGroupName(tenantId, plantCode, lineCode));

    public Task LeaveLineGroup(Guid tenantId, string plantCode, string lineCode)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, BuildGroupName(tenantId, plantCode, lineCode));
}
