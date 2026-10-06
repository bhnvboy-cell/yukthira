using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Infrastructure.Messaging;

public class MrpBusBridge : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MrpBusBridge> _logger;

    public MrpBusBridge(IServiceScopeFactory scopeFactory, ILogger<MrpBusBridge> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.SubscribeAsync<object>("event-driven-mrp-bridge", new BridgeHandler(_scopeFactory, _logger));
            _logger.LogInformation("Event-driven MRP bridge subscribed to message bus");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event-driven MRP bridge subscription failed");
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private sealed class BridgeHandler : IMessageHandler<object>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;

        public BridgeHandler(IServiceScopeFactory scopeFactory, ILogger logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task HandleAsync(MessageEnvelope<object> message)
        {
            try
            {
                if (message?.Payload == null) return;

                var payloadType = message.Payload.GetType();
                var eventTypeProperty = payloadType.GetProperty("eventType");
                var eventType = eventTypeProperty?.GetValue(message.Payload)?.ToString();
                if (!string.Equals(eventType, "order.created", StringComparison.OrdinalIgnoreCase)) return;

                var orderIdProperty = payloadType.GetProperty("orderId");
                var orderIdRaw = orderIdProperty?.GetValue(message.Payload)?.ToString();
                if (!Guid.TryParse(orderIdRaw, out var orderId) || orderId == Guid.Empty) return;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Data.YuktiraDbContext>();
                var engine = scope.ServiceProvider.GetRequiredService<EventDrivenMrpEngine>();

                var lines = await db.SalesOrderLines
                    .Where(l => l.SalesOrderId == orderId)
                    .ToListAsync();

                var evt = new Core.Interfaces.MrpDomainEvent
                {
                    EventType = "SalesOrderCreated",
                    TenantId = message.TenantId,
                    OccurredAt = message.Timestamp,
                    ReferenceId = orderId.ToString(),
                    MaterialCodes = new List<string>(),
                    Quantities = new Dictionary<string, decimal>()
                };

                foreach (var line in lines)
                {
                    var key = line.MaterialName;
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    if (!evt.MaterialCodes.Contains(key)) evt.MaterialCodes.Add(key);
                    evt.Quantities[key] = evt.Quantities.TryGetValue(key, out var existing)
                        ? existing + line.Quantity
                        : line.Quantity;
                }

                if (evt.MaterialCodes.Count == 0) return;

                engine.EnqueueEvent(evt);
                _logger.LogInformation("Bridged sales order {OrderId} to event-driven MRP ({MaterialCount} materials)", orderId, evt.MaterialCodes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MRP bridge failed to process message {MessageId}", message?.Id);
            }
        }
    }
}
