using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly YuktiraDbContext _db;
    private readonly HttpClient _http;

    private static readonly string[] _supportedEvents = new[]
    {
        "document.created", "document.updated", "document.deleted",
        "approval.submitted", "approval.approved", "approval.rejected",
        "workflow.started", "workflow.completed", "workflow.step",
        "qc.result.recorded", "qc.specification.violated",
        "grn.created", "grn.verified",
        "po.created", "po.approved", "po.received",
        "so.created", "so.confirmed", "so.shipped",
        "notification.sent", "integration.sync.completed"
    };

    public WebhookService(YuktiraDbContext db, HttpClient http) { _db = db; _http = http; }

    public async Task DispatchAsync(Guid tenantId, string eventType, string entityType, string entityId, object? payload = null)
    {
        var hooks = await _db.Webhooks
            .Where(w => w.TenantId == tenantId && w.EventType == eventType && w.IsActive)
            .ToListAsync();

        foreach (var hook in hooks)
        {
            var envelope = new
            {
                event_type = eventType,
                tenant_id = tenantId.ToString(),
                entity_type = entityType,
                entity_id = entityId,
                data = payload,
                timestamp = DateTime.UtcNow
            };
            var json = JsonSerializer.Serialize(envelope);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(hook.SecretKey))
                content.Headers.Add("X-Webhook-Secret", hook.SecretKey);

            try
            {
                var resp = await _http.PostAsync(hook.TargetUrl, content);
                hook.LastTriggeredAt = DateTime.UtcNow;
                _db.WebhookDeliveryLogs.Add(new WebhookDeliveryLogEntity
                {
                    TenantId = tenantId, WebhookId = hook.Id, EventType = eventType,
                    TargetUrl = hook.TargetUrl, StatusCode = (int)resp.StatusCode,
                    IsSuccess = resp.IsSuccessStatusCode, AttemptedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _db.WebhookDeliveryLogs.Add(new WebhookDeliveryLogEntity
                {
                    TenantId = tenantId, WebhookId = hook.Id, EventType = eventType,
                    TargetUrl = hook.TargetUrl, StatusCode = 0, IsSuccess = false,
                    ErrorMessage = ex.Message, AttemptedAt = DateTime.UtcNow
                });
            }
        }
        if (hooks.Count > 0) await _db.SaveChangesAsync();
    }

    public async Task<List<WebhookDeliveryLogDto>> GetDeliveryLogsAsync(Guid tenantId, Guid webhookId, int page = 1, int pageSize = 20)
    {
        return await _db.WebhookDeliveryLogs
            .Where(l => l.TenantId == tenantId && l.WebhookId == webhookId)
            .OrderByDescending(l => l.AttemptedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new WebhookDeliveryLogDto
            {
                Id = l.Id, WebhookId = l.WebhookId, EventType = l.EventType,
                TargetUrl = l.TargetUrl, StatusCode = l.StatusCode,
                IsSuccess = l.IsSuccess, ErrorMessage = l.ErrorMessage, AttemptedAt = l.AttemptedAt
            }).ToListAsync();
    }

    public async Task<bool> RetryDeliveryAsync(Guid tenantId, Guid logId)
    {
        var log = await _db.WebhookDeliveryLogs.FirstOrDefaultAsync(l => l.Id == logId && l.TenantId == tenantId);
        if (log == null) return false;
        await DispatchAsync(tenantId, log.EventType, "", "", null);
        return true;
    }

    public Task<List<string>> GetSupportedEventTypesAsync() => Task.FromResult(_supportedEvents.ToList());
}
