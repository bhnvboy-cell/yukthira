using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public class WebhookEvent
{
    public string EventType { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Dictionary<string, object>? Payload { get; set; }
}

public interface IIntegrationHub
{
    Task DispatchWebhookEventAsync(WebhookEvent webhookEvent);
    Task RegisterWebhookAsync(Guid tenantId, string name, string eventType, string targetUrl, string? secretKey = null);
    Task<bool> ValidateApiClientAsync(string clientId, string clientSecret, string ipAddress);
    Task<List<WebhookDto>> GetWebhooksAsync(Guid tenantId);
    Task<bool> DeleteWebhookAsync(Guid webhookId);
}
