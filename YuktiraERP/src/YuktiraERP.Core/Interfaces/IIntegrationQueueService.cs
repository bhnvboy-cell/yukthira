namespace YuktiraERP.Core.Interfaces;

public class IntegrationQueueDto
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = "";
    public string Status { get; set; } = "";
    public int RetryCount { get; set; }
    public string LastError { get; set; } = "";
    public DateTime? NextRetryAt { get; set; }
    public string TargetSystem { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class IntegrationDeadLetterDto
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public int RetryAttempts { get; set; }
    public DateTime FailedAt { get; set; }
}

public interface IIntegrationQueueService
{
    Task EnqueueAsync(Guid tenantId, string messageType, object payload, string targetSystem = "");
    Task<List<IntegrationQueueDto>> GetPendingAsync(Guid tenantId, int limit = 50);
    Task ProcessQueueAsync(Guid tenantId);
    Task<List<IntegrationDeadLetterDto>> GetDeadLetterAsync(Guid tenantId);
    Task RequeueDeadLetterAsync(Guid deadLetterId);
}

public class MessageEnvelope<T>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    public string CorrelationId { get; set; } = "";
    public int RetryCount { get; set; }
    public T Payload { get; set; } = default!;
}

public interface IMessageHandler<T>
{
    Task HandleAsync(MessageEnvelope<T> message);
}

public interface IMessageBus
{
    Task PublishAsync<T>(MessageEnvelope<T> message);
    Task SubscribeAsync<T>(string subscriptionId, IMessageHandler<T> handler);
    Task UnsubscribeAsync<T>(string subscriptionId);
}
