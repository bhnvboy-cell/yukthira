namespace YuktiraERP.Core.Interfaces;

public interface IWebhookService
{
    Task DispatchAsync(Guid tenantId, string eventType, string entityType, string entityId, object? payload = null);
    Task<List<WebhookDeliveryLogDto>> GetDeliveryLogsAsync(Guid tenantId, Guid webhookId, int page = 1, int pageSize = 20);
    Task<bool> RetryDeliveryAsync(Guid tenantId, Guid logId);
    Task<List<string>> GetSupportedEventTypesAsync();
}

public class WebhookDeliveryLogDto
{
    public Guid Id { get; set; }
    public Guid WebhookId { get; set; }
    public string EventType { get; set; } = "";
    public string TargetUrl { get; set; } = "";
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = "";
    public DateTime AttemptedAt { get; set; }
}
