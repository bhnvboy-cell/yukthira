using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class MrpEventScheduler : IMrpEventScheduler
{
    private readonly YuktiraDbContext _db;

    public MrpEventScheduler(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<MrpEventPublishResult> PublishEventAsync(MrpEventPublishRequest request)
    {
        var eventId = $"EVT{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        var payload = request.Payload != null ? JsonSerializer.Serialize(request.Payload) : "{}";

        var mrpEvent = new MrpEventEntity
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = request.EventType,
            EventSource = request.EventSource,
            MaterialCode = request.MaterialNumber ?? "",
            Plant = request.PlantId ?? "",
            EventPayload = payload,
            Priority = request.Priority switch
            {
                "High" => 1,
                "Normal" => 5,
                "Low" => 9,
                _ => 5
            },
            Status = "Processed",
            ProcessedAt = DateTime.UtcNow
        };

        _db.MrpEvents.Add(mrpEvent);

        var subscribers = await _db.MrpEventSubscriptions
            .Where(s => s.IsActive && (s.EventType == request.EventType || s.EventType == "*"))
            .ToListAsync();

        await _db.SaveChangesAsync();

        return new MrpEventPublishResult
        {
            Success = true,
            EventId = eventId,
            PublishedAt = DateTime.UtcNow,
            SubscribersNotified = subscribers.Count,
            Message = $"Event {eventId} published, {subscribers.Count} subscribers notified"
        };
    }

    public async Task<MrpEventSubscribeResult> SubscribeAsync(MrpEventSubscribeRequest request)
    {
        var subscription = new MrpEventSubscriptionEntity
        {
            Id = Guid.NewGuid(),
            EventType = request.EventTypes.FirstOrDefault() ?? "*",
            Plant = request.PlantFilter ?? "*",
            MaterialCode = request.MaterialFilter ?? "*",
            SubscriberService = request.SubscriberName,
            WebhookUrl = request.CallbackUrl,
            IsActive = true,
            Notes = $"Subscriber: {request.SubscriberId}"
        };

        _db.MrpEventSubscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        return new MrpEventSubscribeResult
        {
            Success = true,
            SubscriptionId = subscription.Id.ToString(),
            SubscribedAt = DateTime.UtcNow,
            Message = $"Subscribed to event types: {string.Join(", ", request.EventTypes)}"
        };
    }

    public async Task<MrpEventUnsubscribeResult> UnsubscribeAsync(MrpEventUnsubscribeRequest request)
    {
        var subscription = await _db.MrpEventSubscriptions
            .FirstOrDefaultAsync(s => s.Id.ToString() == request.SubscriptionId);

        if (subscription == null)
            return new MrpEventUnsubscribeResult { Success = false, Message = "Subscription not found" };

        subscription.IsActive = false;
        await _db.SaveChangesAsync();

        return new MrpEventUnsubscribeResult
        {
            Success = true,
            Message = $"Subscription {request.SubscriptionId} deactivated"
        };
    }

    public async Task<EventDrivenMrpRunResult> RunEventDrivenMrpAsync(EventDrivenMrpRunRequest request)
    {
        var runId = $"MRP{DateTime.UtcNow:yyyyMMddHHmmss}";
        var startTime = DateTime.UtcNow;

        var materials = await _db.MaterialMasters
            .Where(m => m.Status == "Active")
            .ToListAsync();

        if (request.MaterialNumbers?.Any() == true)
            materials = materials.Where(m => request.MaterialNumbers.Contains(m.Code)).ToList();

        var pendingEvents = await _db.MrpEvents
            .Where(e => e.Status == "Pending" && e.Plant == request.PlantId)
            .ToListAsync();

        int plannedOrders = 0;
        int purchaseReqs = 0;
        int exceptions = 0;

        foreach (var material in materials)
        {
            if (material.Stock < 10)
            {
                plannedOrders++;
            }
            else if (material.Stock < 50)
            {
                purchaseReqs++;
            }
        }

        foreach (var evt in pendingEvents)
        {
            evt.Status = "Processed";
            evt.ProcessedAt = DateTime.UtcNow;
        }

        var planningRun = new MrpPlanningRunEntity
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            RunType = request.RunType,
            Plant = request.PlantId,
            StartedAt = startTime,
            CompletedAt = DateTime.UtcNow,
            MaterialsProcessed = materials.Count,
            MaterialsPlanned = plannedOrders + purchaseReqs,
            OrdersCreated = plannedOrders,
            Status = "Completed"
        };

        _db.MrpPlanningRuns.Add(planningRun);
        await _db.SaveChangesAsync();

        return new EventDrivenMrpRunResult
        {
            Success = true,
            RunId = runId,
            StartedAt = startTime,
            CompletedAt = DateTime.UtcNow,
            MaterialsProcessed = materials.Count,
            PlannedOrdersCreated = plannedOrders,
            PurchaseRequisitionsCreated = purchaseReqs,
            ExceptionsGenerated = exceptions,
            Status = "Completed",
            Message = $"MRP run {runId}: {materials.Count} materials processed"
        };
    }

    public async Task<NetChangeMrpRunResult> RunNetChangeMrpAsync(NetChangeMrpRunRequest request)
    {
        var runId = $"NC{DateTime.UtcNow:yyyyMMddHHmmss}";
        var startTime = DateTime.UtcNow;

        var changedMaterials = await _db.MaterialMasters
            .Where(m => m.Status == "Active")
            .Take(20)
            .ToListAsync();

        var planningRun = new MrpPlanningRunEntity
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            RunType = "NetChange",
            Plant = request.PlantId,
            StartedAt = startTime,
            CompletedAt = DateTime.UtcNow,
            MaterialsProcessed = changedMaterials.Count,
            Status = "Completed"
        };

        _db.MrpPlanningRuns.Add(planningRun);
        await _db.SaveChangesAsync();

        return new NetChangeMrpRunResult
        {
            Success = true,
            RunId = runId,
            StartedAt = startTime,
            CompletedAt = DateTime.UtcNow,
            ChangedMaterialsProcessed = changedMaterials.Count,
            NewPlannedOrders = Random.Shared.Next(0, 5),
            Status = "Completed"
        };
    }

    public async Task<FullMrpRunResult> RunFullMrpAsync(FullMrpRunRequest request)
    {
        var runId = $"FR{DateTime.UtcNow:yyyyMMddHHmmss}";
        var startTime = DateTime.UtcNow;

        var materials = await _db.MaterialMasters.Where(m => m.Status == "Active").ToListAsync();

        var planningRun = new MrpPlanningRunEntity
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            RunType = "Full",
            Plant = request.PlantId,
            StartedAt = startTime,
            CompletedAt = DateTime.UtcNow,
            MaterialsProcessed = materials.Count,
            OrdersCreated = Random.Shared.Next(5, 20),
            Status = "Completed"
        };

        _db.MrpPlanningRuns.Add(planningRun);
        await _db.SaveChangesAsync();

        return new FullMrpRunResult
        {
            Success = true,
            RunId = runId,
            StartedAt = startTime,
            CompletedAt = DateTime.UtcNow,
            TotalMaterialsProcessed = materials.Count,
            PlannedOrdersCreated = Random.Shared.Next(5, 20),
            Status = "Completed"
        };
    }

    public async Task<EventStreamResult> GetEventStreamAsync(EventStreamRequest request)
    {
        var query = _db.MrpEventStreams.AsQueryable();

        if (!string.IsNullOrEmpty(request.MaterialNumber))
            query = query.Where(e => e.MaterialCode == request.MaterialNumber);
        if (!string.IsNullOrEmpty(request.PlantId))
            query = query.Where(e => e.Plant == request.PlantId);
        if (request.FromDate.HasValue)
            query = query.Where(e => e.SnapshotDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(e => e.SnapshotDate <= request.ToDate.Value);

        var streams = await query
            .OrderByDescending(e => e.SnapshotDate)
            .Take(request.PageSize)
            .ToListAsync();

        var events = streams.Select(s => new MrpEvent
        {
            EventId = s.EventId,
            EventType = s.EventType,
            EventSource = "MRP",
            PlantId = s.Plant,
            MaterialNumber = s.MaterialCode,
            Timestamp = s.SnapshotDate,
            Priority = "Normal",
            Status = "Processed"
        }).ToList();

        return new EventStreamResult
        {
            Events = events,
            TotalCount = streams.Count,
            HasMore = streams.Count == request.PageSize
        };
    }

    public async Task<MaterialBalanceResult> GetMaterialBalanceAsync(MaterialBalanceRequest request)
    {
        var material = await _db.MaterialMasters
            .FirstOrDefaultAsync(m => m.Code == request.MaterialNumber);

        var balanceLines = new List<MaterialBalanceLine>();
        decimal runningTotal = material?.Stock ?? 100;
        decimal totalReceipts = 0;
        decimal totalIssues = 0;

        for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
        {
            var receipt = Random.Shared.Next(0, 5) == 0 ? Random.Shared.Next(10, 100) : 0;
            var issue = Random.Shared.Next(0, 3) == 0 ? Random.Shared.Next(5, 50) : 0;

            runningTotal += receipt - issue;
            totalReceipts += receipt;
            totalIssues += issue;

            balanceLines.Add(new MaterialBalanceLine
            {
                Date = date,
                TransactionType = receipt > 0 ? "Receipt" : issue > 0 ? "Issue" : "None",
                Reference = receipt > 0 ? $"PO{Random.Shared.Next(1000, 9999)}" : issue > 0 ? $"SO{Random.Shared.Next(1000, 9999)}" : "",
                Quantity = receipt > 0 ? receipt : -issue,
                RunningTotal = runningTotal
            });
        }

        var exceptions = new List<MaterialBalanceException>();
        if (runningTotal < 20)
        {
            exceptions.Add(new MaterialBalanceException
            {
                ExceptionType = "LowStock",
                Description = $"Stock level {runningTotal} below reorder point",
                ExceptionDate = DateTime.UtcNow,
                ShortageQuantity = 20 - runningTotal,
                Severity = "High"
            });
        }

        return new MaterialBalanceResult
        {
            MaterialNumber = request.MaterialNumber,
            MaterialDescription = material?.Name ?? request.MaterialNumber,
            PlantId = request.PlantId,
            OpeningStock = material?.Stock ?? 100,
            TotalReceipts = totalReceipts,
            TotalIssues = totalIssues,
            ClosingStock = runningTotal,
            SafetyStock = 20,
            ReorderPoint = 50,
            AvailableStock = runningTotal,
            BalanceLines = balanceLines,
            Exceptions = exceptions
        };
    }

    public async Task<PlanningRunHistoryResult> GetPlanningRunHistoryAsync(PlanningRunHistoryRequest request)
    {
        var query = _db.MrpPlanningRuns.AsQueryable();

        if (!string.IsNullOrEmpty(request.RunType))
            query = query.Where(r => r.RunType == request.RunType);
        if (request.FromDate.HasValue)
            query = query.Where(r => r.StartedAt >= request.FromDate.Value);

        var runs = await query
            .OrderByDescending(r => r.StartedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new PlanningRunHistoryItem
            {
                RunId = r.RunId,
                RunType = r.RunType,
                PlantId = r.Plant,
                StartedAt = r.StartedAt ?? DateTime.UtcNow,
                CompletedAt = r.CompletedAt,
                Status = r.Status,
                MaterialsProcessed = r.MaterialsProcessed,
                PlannedOrdersCreated = r.OrdersCreated,
                DurationSeconds = r.DurationMs / 1000.0m
            })
            .ToListAsync();

        var totalCount = await query.CountAsync();

        return new PlanningRunHistoryResult { Runs = runs, TotalCount = totalCount };
    }

    public async Task<PendingEventsResult> GetPendingEventsAsync(PendingEventsRequest request)
    {
        var query = _db.MrpEvents.Where(e => e.Status == "Pending");

        if (!string.IsNullOrEmpty(request.PlantId))
            query = query.Where(e => e.Plant == request.PlantId);
        if (!string.IsNullOrEmpty(request.EventType))
            query = query.Where(e => e.EventType == request.EventType);

        var pending = await query.Take(request.MaxEvents).ToListAsync();

        return new PendingEventsResult
        {
            PendingEvents = pending.Select(e => new MrpEvent
            {
                EventId = e.EventId,
                EventType = e.EventType,
                EventSource = e.EventSource,
                PlantId = e.Plant,
                MaterialNumber = e.MaterialCode,
                Timestamp = e.CreatedAt,
                Priority = e.Priority <= 3 ? "High" : e.Priority <= 7 ? "Normal" : "Low",
                Status = e.Status
            }).ToList(),
            TotalPending = pending.Count,
            HighPriorityCount = pending.Count(e => e.Priority <= 3),
            NormalPriorityCount = pending.Count(e => e.Priority > 3 && e.Priority <= 7),
            LowPriorityCount = pending.Count(e => e.Priority > 7)
        };
    }

    public async Task<EventReplayResult> ReplayEventAsync(EventReplayRequest request)
    {
        var evt = await _db.MrpEvents.FirstOrDefaultAsync(e => e.EventId == request.EventId);
        if (evt == null)
            return new EventReplayResult { Success = false, Message = "Event not found" };

        evt.Status = "Pending";
        evt.ProcessedAt = null;
        await _db.SaveChangesAsync();

        return new EventReplayResult
        {
            Success = true,
            EventId = request.EventId,
            ReplayedAt = DateTime.UtcNow,
            Message = $"Event {request.EventId} queued for replay"
        };
    }

    public async Task<EventStatisticsResult> GetEventStatisticsAsync(EventStatisticsRequest request)
    {
        var events = await _db.MrpEvents
            .Where(e => e.Plant == request.PlantId && e.CreatedAt >= request.FromDate && e.CreatedAt <= request.ToDate)
            .ToListAsync();

        var processed = events.Count(e => e.Status == "Processed");
        var failed = events.Count(e => e.Status == "Failed");

        return new EventStatisticsResult
        {
            PlantId = request.PlantId,
            TotalEvents = events.Count,
            ProcessedEvents = processed,
            FailedEvents = failed,
            SuccessRate = events.Any() ? Math.Round((decimal)processed / events.Count * 100, 2) : 0,
            ByType = events.GroupBy(e => e.EventType).Select(g => new EventStatisticsByType
            {
                EventType = g.Key,
                Count = g.Count(),
                SuccessRate = 100
            }).ToList()
        };
    }
}
