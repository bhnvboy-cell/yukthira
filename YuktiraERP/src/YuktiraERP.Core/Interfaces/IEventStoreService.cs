using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IEventStoreService
{
    Task AppendEventAsync(DomainEventEnvelope envelope);
    Task<List<DomainEventEnvelope>> GetEventsAsync(Guid aggregateId, int? fromVersion = null);
    Task<List<DomainEventEnvelope>> GetEventsByTypeAsync(string eventType, Guid tenantId, DateTime? from = null, DateTime? to = null);
    Task<T?> GetAggregateAsync<T>(Guid aggregateId, Guid tenantId) where T : class;
    Task<YuktiraERP.Core.Dtos.EventReplayResult> ReplayEventsAsync(YuktiraERP.Core.Dtos.EventReplayRequest request);
    Task<long> GetEventCountAsync(Guid tenantId, string? aggregateType = null);
}

public interface IEventProjectionService
{
    Task ProjectStockBalanceAsync(Guid tenantId, string? materialCode = null);
    Task ProjectMaterialDocumentAsync(Guid tenantId, DateTime? fromDate = null);
    Task<YuktiraERP.Core.Dtos.ReadModelSnapshot> GetReadModelSnapshotAsync(string modelName, Guid tenantId);
    Task SaveReadModelSnapshotAsync(YuktiraERP.Core.Dtos.ReadModelSnapshot snapshot);
}
