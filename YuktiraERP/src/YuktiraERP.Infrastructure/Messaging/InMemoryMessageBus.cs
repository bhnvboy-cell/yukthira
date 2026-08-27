using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Messaging;

public class InMemoryMessageBus : IMessageBus
{
    private readonly ILogger<InMemoryMessageBus> _logger;
    private readonly ConcurrentDictionary<string, Channel<object>> _channels = new();
    private readonly ConcurrentDictionary<string, List<Func<object, Task>>> _handlers = new();
    private readonly ConcurrentQueue<(string MessageType, object Message, DateTime NextRetryAt)> _deadLetterQueue = new();
    private const int MaxRetries = 3;

    public InMemoryMessageBus(ILogger<InMemoryMessageBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(MessageEnvelope<T> message)
    {
        var messageType = typeof(T).Name;
        var channel = _channels.GetOrAdd(messageType, _ => Channel.CreateUnbounded<object>());

        _logger.LogInformation("Publishing message {MessageType} with Id {MessageId}", messageType, message.Id);

        _ = channel.Writer.TryWrite(message);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(string subscriptionId, IMessageHandler<T> handler)
    {
        var messageType = typeof(T).Name;
        var channel = _channels.GetOrAdd(messageType, _ => Channel.CreateUnbounded<object>());

        _handlers.AddOrUpdate(subscriptionId,
            new List<Func<object, Task>> { msg => handler.HandleAsync((MessageEnvelope<T>)msg) },
            (_, existing) =>
            {
                existing.Add(msg => handler.HandleAsync((MessageEnvelope<T>)msg));
                return existing;
            });

        _logger.LogInformation("Subscribed {SubscriptionId} to {MessageType}", subscriptionId, messageType);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync<T>(string subscriptionId)
    {
        _handlers.TryRemove(subscriptionId, out _);
        _logger.LogInformation("Unsubscribed {SubscriptionId}", subscriptionId);
        return Task.CompletedTask;
    }

    public async Task ProcessMessagesAsync<T>(CancellationToken cancellationToken)
    {
        var messageType = typeof(T).Name;
        if (!_channels.TryGetValue(messageType, out var channel))
            return;

        var handlers = _handlers.Values.SelectMany(h => h).ToList();

        while (await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (channel.Reader.TryRead(out var message))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler(message);
                    }
                    catch (Exception ex)
                    {
                        var envelope = (MessageEnvelope<T>)message;
                        _logger.LogError(ex, "Error processing message {MessageId}", envelope.Id);

                        if (envelope.RetryCount < MaxRetries)
                        {
                            envelope.RetryCount++;
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, envelope.RetryCount));
                            _logger.LogWarning("Retrying message {MessageId} in {Delay}s (attempt {RetryCount})",
                                envelope.Id, delay.TotalSeconds, envelope.RetryCount);
                            await Task.Delay(delay, cancellationToken);
                            await PublishAsync(envelope);
                        }
                        else
                        {
                            _deadLetterQueue.Enqueue((messageType, message, DateTime.UtcNow));
                            _logger.LogError("Message {MessageId} moved to dead-letter queue after {MaxRetries} retries",
                                envelope.Id, MaxRetries);
                        }
                    }
                }
            }
        }
    }
}
