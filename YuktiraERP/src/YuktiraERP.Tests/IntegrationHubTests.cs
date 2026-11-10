using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class IntegrationHubTests
{
    [Fact]
    public async Task RegisterWebhook_PersistsToDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new YuktiraDbContext(options);
        var httpClient = new HttpClient();

        var service = new IntegrationHubService(db, new WebhookService(db, httpClient));

        var tenantId = Guid.NewGuid();
        await service.RegisterWebhookAsync(tenantId, "Test Hook", "order.created", "https://example.com/hook", "secret-123");

        var webhooks = await service.GetWebhooksAsync(tenantId);
        Assert.Single(webhooks);
        Assert.Equal("Test Hook", webhooks[0].Name);
        Assert.Equal("order.created", webhooks[0].EventType);
    }

    [Fact]
    public async Task DeleteWebhook_Deactivates()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new YuktiraDbContext(options);
        var httpClient = new HttpClient();

        var service = new IntegrationHubService(db, new WebhookService(db, httpClient));

        var tenantId = Guid.NewGuid();
        await service.RegisterWebhookAsync(tenantId, "Test", "event.test", "https://example.com/hook");

        var webhooks = await service.GetWebhooksAsync(tenantId);
        var id = webhooks[0].Id;

        var deleted = await service.DeleteWebhookAsync(id);
        Assert.True(deleted);

        var after = await service.GetWebhooksAsync(tenantId);
        Assert.False(after[0].IsActive);
    }

    [Fact]
    public async Task ValidateApiClient_ValidatesIp()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new YuktiraDbContext(options);

        db.ApiClients.Add(new ApiClientEntity
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ClientId = "client-1",
            ClientSecret = "secret-1",
            Name = "Test Client",
            AllowedIpAddresses = new[] { "192.168.1.100" },
            IsActive = true
        });
        await db.SaveChangesAsync();

        var httpClient = new HttpClient();
        var service = new IntegrationHubService(db, new WebhookService(db, httpClient));

        var allowed = await service.ValidateApiClientAsync("client-1", "secret-1", "192.168.1.100");
        Assert.True(allowed);

        var denied = await service.ValidateApiClientAsync("client-1", "secret-1", "10.0.0.1");
        Assert.False(denied);
    }

    [Fact]
    public async Task IntegrationQueue_EnqueueAndRequeue_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new YuktiraDbContext(options);
        var busMock = new Mock<IMessageBus>();
        var httpClient = new HttpClient();
        var qsvc = new IntegrationQueueService(db, httpClient, busMock.Object);

        var tenantId = Guid.NewGuid();
        await qsvc.EnqueueAsync(tenantId, "order.sync", new { orderId = "SO-101" }, "EXTERNAL_ERP");

        var pending = await qsvc.GetPendingAsync(tenantId);
        Assert.Single(pending);
        Assert.Equal("order.sync", pending[0].MessageType);
        Assert.Equal("Pending", pending[0].Status);

        // Simulate dead-letter creation
        var deadLetter = new IntegrationDeadLetterEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OriginalQueueId = pending[0].Id,
            MessageType = "order.sync",
            Payload = "{\"orderId\":\"SO-101\"}",
            ErrorMessage = "Target unreachable",
            RetryAttempts = 3,
            FailedAt = DateTime.UtcNow
        };
        db.IntegrationDeadLetters.Add(deadLetter);
        await db.SaveChangesAsync();

        var deadLetters = await qsvc.GetDeadLetterAsync(tenantId);
        Assert.Single(deadLetters);

        // Requeue dead letter
        await qsvc.RequeueDeadLetterAsync(deadLetter.Id);
        var deadAfter = await qsvc.GetDeadLetterAsync(tenantId);
        Assert.Empty(deadAfter);

        var pendingAfter = await qsvc.GetPendingAsync(tenantId);
        Assert.Equal(2, pendingAfter.Count);
    }
}
