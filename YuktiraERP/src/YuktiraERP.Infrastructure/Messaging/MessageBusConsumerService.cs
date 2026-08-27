using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Messaging;

public class MessageBusConsumerService : BackgroundService
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<MessageBusConsumerService> _logger;

    public MessageBusConsumerService(IMessageBus messageBus, ILogger<MessageBusConsumerService> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message bus consumer service started");
        
        if (_messageBus is InMemoryMessageBus inMemoryBus)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await inMemoryBus.ProcessMessagesAsync<object>(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing messages");
                }
                
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
