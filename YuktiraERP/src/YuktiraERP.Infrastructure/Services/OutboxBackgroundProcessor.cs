using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.PluginSdk;

namespace YuktiraERP.Infrastructure.Services;

public class OutboxBackgroundProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxBackgroundProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(3);

    public OutboxBackgroundProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxBackgroundProcessor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxBackgroundProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in OutboxBackgroundProcessor loop.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxBackgroundProcessor stopped.");
    }

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var eventBus = scope.ServiceProvider.GetService<IEventBusRegistry>();

        var messages = await outboxService.GetUnpublishedMessagesAsync(batchSize: 20, cancellationToken);
        if (messages.Count == 0) return 0;

        int processedCount = 0;

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    // Fallback search across loaded assemblies
                    eventType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a =>
                        {
                            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                        })
                        .FirstOrDefault(t => t.FullName == message.EventType || t.Name == message.EventType);
                }

                if (eventType != null && typeof(IDomainEvent).IsAssignableFrom(eventType))
                {
                    if (JsonSerializer.Deserialize(message.PayloadJson, eventType) is IDomainEvent domainEvent && eventBus != null)
                    {
                        await eventBus.PublishAsync(domainEvent, cancellationToken);
                    }
                }

                await outboxService.MarkPublishedAsync(message.Id, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId} of type {EventType}", message.Id, message.EventType);
                await outboxService.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }

        return processedCount;
    }
}
