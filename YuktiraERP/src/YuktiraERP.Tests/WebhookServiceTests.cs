using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class WebhookServiceTests
{
    private static YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task RetryDelivery_RedeliversOnlyTheSpecificWebhook()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var target = new WebhookEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Target",
            EventType = "order.created",
            TargetUrl = "http://127.0.0.1:1/none",
            IsActive = true
        };
        var other = new WebhookEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Other",
            EventType = "order.created",
            TargetUrl = "http://127.0.0.1:1/other",
            IsActive = true
        };
        db.Webhooks.AddRange(target, other);
        await db.SaveChangesAsync();

        var log = new WebhookDeliveryLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WebhookId = target.Id,
            EventType = "order.created",
            TargetUrl = target.TargetUrl,
            StatusCode = 500,
            IsSuccess = false,
            AttemptedAt = DateTime.UtcNow
        };
        db.WebhookDeliveryLogs.Add(log);
        await db.SaveChangesAsync();

        var service = new WebhookService(db, new HttpClient());
        var retried = await service.RetryDeliveryAsync(tenantId, log.Id);
        Assert.True(retried);

        var newLogs = await db.WebhookDeliveryLogs.Where(l => l.Id != log.Id).ToListAsync();
        Assert.Single(newLogs);
        Assert.Equal(target.Id, newLogs[0].WebhookId);
        Assert.Equal(target.TargetUrl, newLogs[0].TargetUrl);
    }

    [Fact]
    public async Task RetryDelivery_ReturnsFalse_WhenLogMissing()
    {
        var db = CreateDb();
        var service = new WebhookService(db, new HttpClient());
        var retried = await service.RetryDeliveryAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(retried);
    }

    [Fact]
    public async Task RetryDelivery_ReturnsFalse_WhenWebhookInactive()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var hook = new WebhookEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Inactive",
            EventType = "order.created",
            TargetUrl = "http://127.0.0.1:1/none",
            IsActive = false
        };
        db.Webhooks.Add(hook);
        var log = new WebhookDeliveryLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WebhookId = hook.Id,
            EventType = "order.created",
            TargetUrl = hook.TargetUrl,
            StatusCode = 500,
            IsSuccess = false,
            AttemptedAt = DateTime.UtcNow
        };
        db.WebhookDeliveryLogs.Add(log);
        await db.SaveChangesAsync();

        var service = new WebhookService(db, new HttpClient());
        var retried = await service.RetryDeliveryAsync(tenantId, log.Id);
        Assert.False(retried);
    }

    [Fact]
    public async Task SupportedEvents_IncludesOrderEvents()
    {
        var db = CreateDb();
        var service = new WebhookService(db, new HttpClient());
        var events = await service.GetSupportedEventTypesAsync();
        Assert.Contains("order.created", events);
        Assert.Contains("order.updated", events);
        Assert.Contains("order.shipped", events);
    }
}