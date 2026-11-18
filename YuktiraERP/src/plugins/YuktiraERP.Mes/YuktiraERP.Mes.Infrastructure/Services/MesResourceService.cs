using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Core.Services;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesResourceService
{
    private readonly MesDbContext _db;

    public MesResourceService(MesDbContext db) => _db = db;

    public async Task<MesResource> CreateResourceAsync(
        Guid tenantId, string plantCode, string code, string name,
        ResourceType type, string erpRefType, Guid? erpRefId, CancellationToken ct = default)
    {
        var resource = new MesResource
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            ResourceCode = code,
            Name = name,
            ResourceType = type,
            ErpRefType = erpRefType,
            ErpRefId = erpRefId,
            CurrentState = ResourceState.IDLE,
            StateSince = DateTime.UtcNow
        };
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(ct);
        return resource;
    }

    public async Task<IReadOnlyList<MesResource>> GetResourcesAsync(Guid tenantId, string? plantCode = null, CancellationToken ct = default)
    {
        var query = _db.Resources.Where(r => r.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(plantCode))
            query = query.Where(r => r.PlantCode == plantCode);
        return await query.OrderBy(r => r.ResourceCode).ToListAsync(ct);
    }

    public async Task<MesResourceStateLog> TransitionStateAsync(
        Guid tenantId, Guid resourceId, ResourceState toState,
        string? reasonCode, StateChangeSource source, string actor, CancellationToken ct = default)
    {
        var resource = await _db.Resources.FirstOrDefaultAsync(r => r.Id == resourceId && r.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Resource {resourceId} not found.");

        var now = DateTime.UtcNow;
        var fromState = resource.CurrentState;

        // Close previous open state log if any
        var openLog = await _db.ResourceStateLogs
            .Where(l => l.TenantId == tenantId && l.ResourceId == resourceId && l.EndedAt == null)
            .OrderByDescending(l => l.StartedAt)
            .FirstOrDefaultAsync(ct);

        if (openLog != null)
        {
            openLog.EndedAt = now;
            openLog.DurationSeconds = Math.Max(0, (now - openLog.StartedAt).TotalSeconds);
        }

        resource.CurrentState = toState;
        resource.StateSince = now;
        resource.RowVersion++;

        var newLog = new MesResourceStateLog
        {
            TenantId = tenantId,
            PlantCode = resource.PlantCode,
            ResourceId = resourceId,
            FromState = fromState,
            ToState = toState,
            ReasonCode = reasonCode,
            ChangeSource = source,
            StartedAt = now,
            CreatedBy = actor
        };
        _db.ResourceStateLogs.Add(newLog);

        // Emit resource.down outbox event if unplanned downtime
        if (toState == ResourceState.DOWN_UNPLANNED)
        {
            _db.OutboxEvents.Add(new MesOutboxEvent
            {
                TenantId = tenantId,
                PlantCode = resource.PlantCode,
                EventType = "resource.down",
                AggregateType = nameof(MesResource),
                AggregateId = resource.Id,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    resource.Id,
                    resource.ResourceCode,
                    reasonCode,
                    occurredAt = now
                })
            });
        }

        await _db.SaveChangesAsync(ct);
        return newLog;
    }

    public async Task<(bool PreAlert, bool InterlockBlocked)> IncrementToolCyclesAsync(
        Guid tenantId, Guid toolResourceId, long cyclesDelta, CancellationToken ct = default)
    {
        var tool = await _db.ToolLives.FirstOrDefaultAsync(t => t.TenantId == tenantId && t.ToolResourceId == toolResourceId, ct)
            ?? throw new KeyNotFoundException($"ToolLife for resource {toolResourceId} not found.");

        tool.CyclesUsed += cyclesDelta;
        var (preAlert, interlock) = ResourceStateEngine.EvaluateToolLife(tool.CyclesUsed, tool.LifeLimit);

        if (interlock)
        {
            _db.OutboxEvents.Add(new MesOutboxEvent
            {
                TenantId = tenantId,
                PlantCode = tool.PlantCode,
                EventType = "tool.life.limit_reached",
                AggregateType = nameof(MesToolLife),
                AggregateId = tool.Id,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { tool.ToolResourceId, tool.CyclesUsed, tool.LifeLimit, tool.BlockAtLimit })
            });
        }

        await _db.SaveChangesAsync(ct);
        return (preAlert, interlock && tool.BlockAtLimit);
    }

    public async Task<IReadOnlyList<MesResource>> FindAlternateResourcesAsync(
        Guid tenantId, string capabilityCode, Guid excludeResourceId, CancellationToken ct = default)
    {
        var capableIds = await _db.ResourceCapabilities
            .Where(c => c.TenantId == tenantId && c.CapabilityCode == capabilityCode && c.ResourceId != excludeResourceId)
            .Select(c => c.ResourceId)
            .Distinct()
            .ToListAsync(ct);

        return await _db.Resources
            .Where(r => r.TenantId == tenantId && capableIds.Contains(r.Id)
                && r.CurrentState != ResourceState.DOWN_UNPLANNED
                && r.CurrentState != ResourceState.DOWN_PLANNED)
            .ToListAsync(ct);
    }
}
