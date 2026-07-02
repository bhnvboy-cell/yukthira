namespace YuktiraERP.Core.Interfaces;

public interface ILiveUpdateService
{
    Task SendWorkflowUpdateAsync(Guid tenantId, Guid instanceId, string status, string message);
    Task SendMrpProgressAsync(Guid tenantId, int percentage, string message);
    Task SendDashboardRefreshAsync(Guid tenantId);
    Task SendNotificationAsync(Guid tenantId, string userId, string title, string message);
}
