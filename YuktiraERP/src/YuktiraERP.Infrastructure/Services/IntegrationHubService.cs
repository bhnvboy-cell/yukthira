using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class IntegrationHubService : IIntegrationHub
{
    private readonly YuktiraDbContext _db;
    private readonly IWebhookService _webhookService;

    public IntegrationHubService(YuktiraDbContext db, IWebhookService webhookService)
    {
        _db = db;
        _webhookService = webhookService;
    }

    public Task DispatchWebhookEventAsync(WebhookEvent webhookEvent)
        => _webhookService.DispatchAsync(webhookEvent.TenantId, webhookEvent.EventType, webhookEvent.EntityType, webhookEvent.EntityId, webhookEvent.Payload);

    public async Task RegisterWebhookAsync(Guid tenantId, string name, string eventType, string targetUrl, string? secretKey = null)
    {
        _db.Webhooks.Add(new WebhookEntity
        {
            TenantId = tenantId,
            Name = name,
            EventType = eventType,
            TargetUrl = targetUrl,
            SecretKey = secretKey ?? "",
            IsActive = true,
            RetryCount = 3
        });
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ValidateApiClientAsync(string clientId, string clientSecret, string ipAddress)
    {
        var client = await _db.ApiClients
            .FirstOrDefaultAsync(c => c.ClientId == clientId && c.ClientSecret == clientSecret && c.IsActive);
        if (client == null) return false;
        if (client.AllowedIpAddresses.Length > 0 && !client.AllowedIpAddresses.Contains(ipAddress))
            return false;
        return true;
    }

    public async Task<List<WebhookDto>> GetWebhooksAsync(Guid tenantId)
    {
        return await _db.Webhooks
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WebhookDto
            {
                Id = w.Id,
                Name = w.Name,
                EventType = w.EventType,
                TargetUrl = w.TargetUrl,
                IsActive = w.IsActive,
                RetryCount = w.RetryCount,
                LastTriggeredAt = w.LastTriggeredAt,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteWebhookAsync(Guid webhookId)
    {
        var hook = await _db.Webhooks.FindAsync(webhookId);
        if (hook == null) return false;
        hook.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }
}
