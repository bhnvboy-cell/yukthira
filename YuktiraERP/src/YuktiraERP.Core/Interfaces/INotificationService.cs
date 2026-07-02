namespace YuktiraERP.Core.Interfaces;

public enum NotificationChannelType { Email, SMS, InApp, All }

public class SendNotificationRequest
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public NotificationChannelType Channel { get; set; } = NotificationChannelType.InApp;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public string? TemplateCode { get; set; }
    public Dictionary<string, string>? TemplateData { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface INotificationService
{
    Task SendAsync(SendNotificationRequest request);
    Task SendToRoleAsync(Guid tenantId, string roleCode, SendNotificationRequest request);
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
}
