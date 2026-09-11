using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class EventStoreService : IEventStoreService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<EventStoreService> _logger;

    public EventStoreService(YuktiraDbContext db, ILogger<EventStoreService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AppendEventAsync(DomainEventEnvelope envelope)
    {
        try
        {
            var latestVersion = await _db.DomainEvents
                .Where(e => e.AggregateId == envelope.AggregateId && e.TenantId == envelope.TenantId)
                .MaxAsync(e => (int?)e.Version) ?? 0;

            envelope.Version = latestVersion + 1;
            envelope.Timestamp = DateTime.UtcNow;
            envelope.EventId = envelope.EventId == Guid.Empty ? Guid.NewGuid() : envelope.EventId;

            var entity = new DomainEventEntity
            {
                Id = envelope.EventId,
                AggregateId = envelope.AggregateId,
                AggregateType = envelope.AggregateType,
                EventType = envelope.EventType,
                EventData = envelope.EventData,
                Version = envelope.Version,
                Timestamp = envelope.Timestamp,
                TenantId = envelope.TenantId,
                UserId = envelope.UserId
            };

            _db.DomainEvents.Add(entity);
            await _db.SaveChangesAsync();

            _logger.LogDebug(
                "Event appended: AggregateId={AggregateId}, Type={EventType}, Version={Version}, Tenant={TenantId}",
                envelope.AggregateId, envelope.EventType, envelope.Version, envelope.TenantId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict appending event for aggregate {AggregateId}", envelope.AggregateId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append event for aggregate {AggregateId}", envelope.AggregateId);
            throw;
        }
    }

    public async Task<List<DomainEventEnvelope>> GetEventsAsync(Guid aggregateId, int? fromVersion = null)
    {
        IQueryable<DomainEventEntity> query = _db.DomainEvents
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version);

        if (fromVersion.HasValue)
            query = query.Where(e => e.Version >= fromVersion.Value);

        var entities = await query.ToListAsync();

        return entities.Select(e => new DomainEventEnvelope
        {
            EventId = e.Id,
            AggregateId = e.AggregateId,
            AggregateType = e.AggregateType,
            EventType = e.EventType,
            EventData = e.EventData,
            Version = e.Version,
            Timestamp = e.Timestamp,
            TenantId = e.TenantId,
            UserId = e.UserId
        }).ToList();
    }

    public async Task<List<DomainEventEnvelope>> GetEventsByTypeAsync(
        string eventType, Guid tenantId, DateTime? from = null, DateTime? to = null)
    {
        IQueryable<DomainEventEntity> query = _db.DomainEvents
            .Where(e => e.EventType == eventType && e.TenantId == tenantId)
            .OrderBy(e => e.Timestamp);

        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.Timestamp <= to.Value);

        var entities = await query.ToListAsync();

        return entities.Select(e => new DomainEventEnvelope
        {
            EventId = e.Id,
            AggregateId = e.AggregateId,
            AggregateType = e.AggregateType,
            EventType = e.EventType,
            EventData = e.EventData,
            Version = e.Version,
            Timestamp = e.Timestamp,
            TenantId = e.TenantId,
            UserId = e.UserId
        }).ToList();
    }

    public async Task<T?> GetAggregateAsync<T>(Guid aggregateId, Guid tenantId) where T : class
    {
        var events = await GetEventsAsync(aggregateId);
        if (!events.Any()) return null;

        var latestEvent = events.Last();
        try
        {
            return JsonSerializer.Deserialize<T>(latestEvent.EventData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize aggregate {AggregateId} to {Type}", aggregateId, typeof(T).Name);
            return null;
        }
    }

    public async Task<YuktiraERP.Core.Dtos.EventReplayResult> ReplayEventsAsync(YuktiraERP.Core.Dtos.EventReplayRequest request)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Starting event replay: Tenant={TenantId}, From={FromDate}, To={ToDate}, AggregateType={AggregateType}",
            request.TenantId, request.FromDate, request.ToDate, request.AggregateType);

        try
        {
            IQueryable<DomainEventEntity> query = _db.DomainEvents
                .Where(e => e.TenantId == request.TenantId)
                .OrderBy(e => e.Timestamp);

            if (request.FromDate.HasValue)
                query = query.Where(e => e.Timestamp >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(e => e.Timestamp <= request.ToDate.Value);
            if (request.AggregateType.HasValue)
                query = query.Where(e => e.AggregateType == request.AggregateType.Value);
            if (!string.IsNullOrEmpty(request.EventType))
                query = query.Where(e => e.EventType == request.EventType);
            if (request.MaxEvents.HasValue)
                query = query.Take(request.MaxEvents.Value);

            var events = await query.ToListAsync();

            var projectedModels = new HashSet<string>();
            foreach (var evt in events)
            {
                projectedModels.Add(evt.AggregateType.ToString());
            }

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Event replay completed: {EventCount} events replayed across {ModelCount} models in {Duration}",
                events.Count, projectedModels.Count, duration);

            return new YuktiraERP.Core.Dtos.EventReplayResult
            {
                Success = true,
                TotalEventsReplayed = events.Count,
                ProjectedModels = projectedModels.ToList(),
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Event replay failed");
            return new YuktiraERP.Core.Dtos.EventReplayResult
            {
                Success = false,
                Duration = DateTime.UtcNow - startTime,
                Errors = { ex.Message }
            };
        }
    }

    public async Task<long> GetEventCountAsync(Guid tenantId, string? aggregateType = null)
    {
        var query = _db.DomainEvents.Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrEmpty(aggregateType) && Enum.TryParse<AggregateType>(aggregateType, true, out var parsed))
            query = query.Where(e => e.AggregateType == parsed);

        return await query.LongCountAsync();
    }
}

public class EventProjectionService : IEventProjectionService
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<EventProjectionService> _logger;

    public EventProjectionService(YuktiraDbContext db, ILogger<EventProjectionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProjectStockBalanceAsync(Guid tenantId, string? materialCode = null)
    {
        _logger.LogInformation("Projecting stock balance: Tenant={TenantId}, Material={Material}",
            tenantId, materialCode ?? "ALL");

        var query = _db.StockBalances.Where(s => s.TenantId == tenantId);
        if (!string.IsNullOrEmpty(materialCode))
            query = query.Where(s => s.MaterialCode == materialCode);

        var stocks = await query.ToListAsync();

        foreach (var stock in stocks)
        {
            var events = await _db.DomainEvents
                .Where(e => e.AggregateId == stock.Id && e.TenantId == tenantId)
                .OrderBy(e => e.Version)
                .ToListAsync();

            var currentQuantity = stock.Quantity;
            _logger.LogDebug("Stock projection: Material={Material}, Quantity={Quantity}, Events={EventCount}",
                stock.MaterialCode, currentQuantity, events.Count);
        }
    }

    public async Task ProjectMaterialDocumentAsync(Guid tenantId, DateTime? fromDate = null)
    {
        _logger.LogInformation("Projecting material documents: Tenant={TenantId}, From={FromDate}",
            tenantId, fromDate?.ToString("O") ?? "ALL");

        var query = _db.MaterialDocumentHeaders
            .Where(m => m.TenantId == tenantId.ToString());

        if (fromDate.HasValue)
            query = query.Where(m => m.CreatedAt >= fromDate.Value);

        var documents = await query.OrderByDescending(m => m.CreatedAt).Take(1000).ToListAsync();

        _logger.LogInformation("Projected {Count} material documents", documents.Count);
    }

    public async Task<YuktiraERP.Core.Dtos.ReadModelSnapshot> GetReadModelSnapshotAsync(string modelName, Guid tenantId)
    {
        var existing = await _db.ReadModelSnapshots
            .FirstOrDefaultAsync(s => s.ModelName == modelName && s.TenantId == tenantId);

        if (existing != null)
        {
            return new YuktiraERP.Core.Dtos.ReadModelSnapshot
            {
                Id = existing.Id,
                ModelName = existing.ModelName,
                TenantId = existing.TenantId,
                ModelData = existing.ModelData,
                Version = existing.Version,
                Timestamp = existing.Timestamp,
                Status = existing.Status
            };
        }

        return new YuktiraERP.Core.Dtos.ReadModelSnapshot
        {
            ModelName = modelName,
            TenantId = tenantId,
            Version = 0,
            Status = ProjectionStatus.Current
        };
    }

    public async Task SaveReadModelSnapshotAsync(YuktiraERP.Core.Dtos.ReadModelSnapshot snapshot)
    {
        var existing = await _db.ReadModelSnapshots
            .FirstOrDefaultAsync(s => s.ModelName == snapshot.ModelName && s.TenantId == snapshot.TenantId);

        if (existing != null)
        {
            existing.ModelData = snapshot.ModelData;
            existing.Version = snapshot.Version;
            existing.Timestamp = DateTime.UtcNow;
            existing.Status = snapshot.Status;
        }
        else
        {
            _db.ReadModelSnapshots.Add(new ReadModelSnapshotEntity
            {
                Id = Guid.NewGuid(),
                ModelName = snapshot.ModelName,
                TenantId = snapshot.TenantId,
                ModelData = snapshot.ModelData,
                Version = snapshot.Version,
                Timestamp = DateTime.UtcNow,
                Status = snapshot.Status
            });
        }

        await _db.SaveChangesAsync();
    }
}
