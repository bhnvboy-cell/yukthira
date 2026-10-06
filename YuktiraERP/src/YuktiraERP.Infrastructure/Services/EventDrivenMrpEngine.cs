using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class EventDrivenMrpEngine : IMrpEventListener, IDisposable
{
    private static readonly HashSet<string> InHouseTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FINISHED", "FG", "FERT", "HALF", "HALB", "SEMI", "SEMBL", "ASSEMBLY"
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MrpEngineOptions _options;
    private readonly ILogger<EventDrivenMrpEngine>? _logger;
    private readonly Channel<MrpDomainEvent> _queue = Channel.CreateUnbounded<MrpDomainEvent>();
    private readonly CancellationTokenSource _drainCts = new();
    private readonly SemaphoreSlim _drainGate = new(1, 1);
    private readonly ConcurrentQueue<MrpEngineEventSummary> _recent = new();
    private readonly object _statsLock = new();
    private Task? _drainTask;
    private long _eventsHandled;
    private long _draftPoCreated;
    private long _draftProdCreated;
    private long _shortages;
    private DateTime? _lastEventAt;

    public EventDrivenMrpEngine(
        IServiceScopeFactory scopeFactory,
        IOptions<MrpEngineOptions> options,
        ILogger<EventDrivenMrpEngine>? logger = null)
    {
        _scopeFactory = scopeFactory;
        _options = options?.Value ?? new MrpEngineOptions();
        _logger = logger;
    }

    public void EnqueueEvent(MrpDomainEvent evt)
    {
        if (evt == null) return;
        _queue.Writer.TryWrite(evt);
        StartDrainLoop();
    }

    public int QueueDepth => _queue.Reader.Count;

    public MrpEngineStats GetStats()
    {
        lock (_statsLock)
        {
            return new MrpEngineStats
            {
                Enabled = _options.Enabled,
                RegisteredListeners = 1,
                EventsHandled = Interlocked.Read(ref _eventsHandled),
                DraftPurchaseOrdersCreated = Interlocked.Read(ref _draftPoCreated),
                DraftProductionOrdersCreated = Interlocked.Read(ref _draftProdCreated),
                ShortagesDetected = Interlocked.Read(ref _shortages),
                QueuedEvents = QueueDepth,
                DrainLoopRunning = _drainTask is { IsCompleted: false },
                LastEventAt = _lastEventAt,
                RecentEvents = _recent.ToList()
            };
        }
    }

    public async Task<MrpEventResult> HandleAsync(MrpDomainEvent evt, CancellationToken ct = default)
    {
        evt ??= new MrpDomainEvent();
        var result = new MrpEventResult
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = evt.EventType,
            TenantId = evt.TenantId
        };

        if (!_options.Enabled)
        {
            result.SkippedReasons.Add("engine-disabled");
            RecordStats(evt, result);
            return result;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();
        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var numberRange = scope.ServiceProvider.GetRequiredService<INumberRangeService>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var materialCodes = evt.MaterialCodes.Count > 0
            ? evt.MaterialCodes.Distinct().ToList()
            : evt.Quantities?.Keys.Distinct().ToList() ?? new List<string>();

        foreach (var code in materialCodes)
        {
            ct.ThrowIfCancellationRequested();
            result.EvaluatedMaterials++;

            var material = await db.MaterialMasters
                .FirstOrDefaultAsync(m => m.Code == code || m.Name == code, ct);
            if (material == null)
            {
                result.SkippedReasons.Add($"{code}:material-not-found");
                continue;
            }

            var required = ResolveRequiredQuantity(evt, material);
            if (required <= 0)
            {
                result.SkippedReasons.Add($"{material.Code}:no-demand-quantity");
                continue;
            }

            var atp = await inventory.CheckAvailabilityAsync(
                material.Id, required, evt.OccurredAt.AddDays(_options.DefaultLeadTimeDays));
            var available = atp.AvailableQuantity;
            var shortage = required - available;
            if (shortage <= 0)
            {
                result.SkippedReasons.Add($"{material.Code}:sufficient-stock");
                continue;
            }

            var refKey = ReferenceKey(evt);
            var inHouse = await IsInHouseAsync(db, material, ct);
            var shortageEntry = new MrpShortage
            {
                MaterialCode = material.Code,
                MaterialName = material.Name,
                RequiredQuantity = required,
                AvailableQuantity = available,
                ShortageQuantity = shortage,
                ProcurementType = inHouse ? "PRODUCTION" : "PURCHASE",
                ReferenceId = evt.ReferenceId
            };
            Interlocked.Increment(ref _shortages);

            if (inHouse)
            {
                var duplicate = await db.ProductionOrders.AnyAsync(
                    p => p.TenantId == evt.TenantId
                        && p.MaterialCode == material.Code
                        && p.BatchNo == refKey
                        && p.Status == "PLANNED", ct);
                if (duplicate)
                {
                    shortageEntry.ActionTaken = "SKIPPED";
                    result.Shortages.Add(shortageEntry);
                    result.SkippedReasons.Add($"{material.Code}:duplicate-draft-production-order");
                    continue;
                }

                var qty = ResolveOrderQuantity(db, material, shortage);
                var orderNumber = await numberRange.GetNextNumberAsync(evt.TenantId, "PP", "PRD");
                var leadDays = await ResolveLeadTimeDaysAsync(db, material.Code, ct);
                var bom = await db.BillOfMaterials
                    .FirstOrDefaultAsync(b => (b.ProductName == material.Name || b.MaterialCode == material.Code)
                        && b.Status == "Active", ct);

                var productionOrder = new ProductionOrderEntity
                {
                    TenantId = evt.TenantId,
                    OrderNumber = orderNumber,
                    OrderType = "PP01",
                    ProductName = material.Name,
                    MaterialCode = material.Code,
                    Quantity = qty,
                    BaseUOM = material.UOM,
                    StartDate = evt.OccurredAt,
                    EndDate = evt.OccurredAt.AddDays(leadDays),
                    Status = "PLANNED",
                    BOMId = bom?.Id,
                    BatchNo = refKey
                };
                db.ProductionOrders.Add(productionOrder);
                await db.SaveChangesAsync(ct);

                result.DraftProductionOrders.Add(orderNumber);
                shortageEntry.ActionTaken = "DRAFT_PRODUCTION_ORDER";
                result.Shortages.Add(shortageEntry);
                Interlocked.Increment(ref _draftProdCreated);
                await LogAuditAsync(audit, evt, "ProductionOrder", productionOrder.Id, orderNumber, material, qty, ct);
            }
            else
            {
                var covered = await db.ProductionOrders.AnyAsync(
                    p => p.MaterialCode == material.Code
                        && (p.Status == "PLANNED" || p.Status == "RELEASED" || p.Status == "IN_PROGRESS"), ct);
                if (covered)
                {
                    shortageEntry.ActionTaken = "SKIPPED";
                    result.Shortages.Add(shortageEntry);
                    result.SkippedReasons.Add($"{material.Code}:covered-by-active-production");
                    continue;
                }

                var duplicateItem = await db.PurchaseOrderItems
                    .FirstOrDefaultAsync(i => i.MaterialCode == material.Code && i.BatchNo == refKey, ct);
                if (duplicateItem != null)
                {
                    var duplicatePo = await db.PurchaseOrders
                        .FirstOrDefaultAsync(p => p.Id == duplicateItem.PurchaseOrderId
                            && p.Status == "DRAFT" && p.TenantId == evt.TenantId, ct);
                    if (duplicatePo != null)
                    {
                        shortageEntry.ActionTaken = "SKIPPED";
                        result.Shortages.Add(shortageEntry);
                        result.SkippedReasons.Add($"{material.Code}:duplicate-draft-po");
                        continue;
                    }
                }

                var vendor = await db.Vendors
                    .Where(v => v.Status == "Active")
                    .OrderBy(v => v.Code)
                    .FirstOrDefaultAsync(ct);
                var qty = ResolveOrderQuantity(db, material, shortage);
                var unitPrice = material.Price;
                var total = Math.Round(qty * unitPrice, 2);
                var leadDays = await ResolveLeadTimeDaysAsync(db, material.Code, ct);
                var poNumber = await numberRange.GetNextNumberAsync(evt.TenantId, "MM", "PO");

                var purchaseOrder = new PurchaseOrderEntity
                {
                    TenantId = evt.TenantId,
                    PoNumber = poNumber,
                    Date = DateTime.UtcNow,
                    VendorName = vendor?.Name ?? "",
                    VendorCode = vendor?.Code ?? "",
                    ItemName = material.Name,
                    Quantity = qty.ToString(),
                    Amount = total,
                    TotalAmount = total,
                    ItemCount = 1,
                    Status = "DRAFT",
                    PaymentTerms = vendor?.PaymentTerms ?? "Net 30"
                };
                db.PurchaseOrders.Add(purchaseOrder);
                db.PurchaseOrderItems.Add(new PurchaseOrderItemEntity
                {
                    TenantId = evt.TenantId,
                    PurchaseOrderId = purchaseOrder.Id,
                    LineNumber = 1,
                    MaterialName = material.Name,
                    MaterialCode = material.Code,
                    Quantity = qty,
                    UOM = material.UOM,
                    UnitPrice = unitPrice,
                    TotalPrice = total,
                    DeliveryDate = evt.OccurredAt.AddDays(leadDays).ToString("yyyy-MM-dd"),
                    Status = "OPEN",
                    BatchNo = refKey
                });
                await db.SaveChangesAsync(ct);

                result.DraftPurchaseOrders.Add(poNumber);
                shortageEntry.ActionTaken = "DRAFT_PO";
                result.Shortages.Add(shortageEntry);
                Interlocked.Increment(ref _draftPoCreated);
                await LogAuditAsync(audit, evt, "PurchaseOrder", purchaseOrder.Id, poNumber, material, qty, ct);
            }
        }

        result.Totals = new MrpEventTotals
        {
            EvaluatedMaterials = result.EvaluatedMaterials,
            ShortageCount = result.Shortages.Count,
            TotalShortageQuantity = result.Shortages.Sum(s => s.ShortageQuantity),
            DraftPurchaseOrders = result.DraftPurchaseOrders.Count,
            DraftProductionOrders = result.DraftProductionOrders.Count,
            SkippedMaterials = result.SkippedReasons.Count
        };

        RecordStats(evt, result);
        return result;
    }

    private void StartDrainLoop()
    {
        if (_drainTask is { IsCompleted: false }) return;
        if (_drainGate.Wait(0))
        {
            try
            {
                if (_drainTask is { IsCompleted: false }) return;
                _drainTask = Task.Run(() => DrainLoopAsync(_drainCts.Token));
            }
            finally
            {
                _drainGate.Release();
            }
        }
    }

    private async Task DrainLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                while (_queue.Reader.TryRead(out var evt))
                {
                    await HandleAsync(evt, ct);
                }

                if (await _queue.Reader.WaitToReadAsync(ct))
                {
                    continue;
                }

                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Event driven MRP drain loop failed");
                var delay = TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds));
                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private void RecordStats(MrpDomainEvent evt, MrpEventResult result)
    {
        var summary = new MrpEngineEventSummary
        {
            EventId = result.EventId,
            EventType = evt.EventType,
            TenantId = evt.TenantId,
            OccurredAt = evt.OccurredAt,
            ProcessedAt = result.ProcessedAt,
            EvaluatedMaterials = result.EvaluatedMaterials,
            DraftPurchaseOrders = result.DraftPurchaseOrders.Count,
            DraftProductionOrders = result.DraftProductionOrders.Count,
            SkippedCount = result.SkippedReasons.Count
        };

        lock (_statsLock)
        {
            Interlocked.Increment(ref _eventsHandled);
            _lastEventAt = DateTime.UtcNow;
            _recent.Enqueue(summary);
            while (_recent.Count > Math.Max(1, _options.MaxRecentEvents) && _recent.TryDequeue(out _))
            {
            }
        }
    }

    private static decimal ResolveRequiredQuantity(MrpDomainEvent evt, MaterialMasterEntity material)
    {
        if (evt.Quantities == null || evt.Quantities.Count == 0) return 0;
        if (evt.Quantities.TryGetValue(material.Code, out var byCode)) return byCode;
        if (evt.Quantities.TryGetValue(material.Name, out var byName)) return byName;
        foreach (var pair in evt.Quantities)
        {
            if (string.Equals(pair.Key, material.Code, StringComparison.OrdinalIgnoreCase)
                || string.Equals(pair.Key, material.Name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }
        return 0;
    }

    private static string ReferenceKey(MrpDomainEvent evt)
    {
        var reference = string.IsNullOrWhiteSpace(evt.ReferenceId)
            ? Guid.NewGuid().ToString("N")
            : evt.ReferenceId;
        return $"MRP:{reference}";
    }

    private static async Task<bool> IsInHouseAsync(YuktiraDbContext db, MaterialMasterEntity material, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(material.Type) && InHouseTypes.Contains(material.Type.Trim()))
            return true;

        return await db.BillOfMaterials.AnyAsync(
            b => (b.ProductName == material.Name || b.MaterialCode == material.Code) && b.Status == "Active", ct);
    }

    private static decimal ResolveOrderQuantity(YuktiraDbContext db, MaterialMasterEntity material, decimal shortage)
    {
        var conversion = db.MaterialUomConversions.AsNoTracking()
            .FirstOrDefault(c => c.MaterialCode == material.Code && c.IsActive && c.ConversionFactor > 1);
        if (conversion == null || conversion.ConversionFactor <= 0) return shortage;

        var packSize = conversion.ConversionFactor;
        return Math.Ceiling(shortage / packSize) * packSize;
    }

    private static async Task<int> ResolveLeadTimeDaysAsync(YuktiraDbContext db, string materialCode, CancellationToken ct)
    {
        var leadTime = await db.VendorLeadTimes.AsNoTracking()
            .Where(v => v.MaterialCode == materialCode)
            .OrderByDescending(v => v.Reliability)
            .Select(v => v.LeadTimeDays)
            .FirstOrDefaultAsync(ct);
        return leadTime > 0 ? leadTime : 7;
    }

    private async Task LogAuditAsync(
        IAuditService audit,
        MrpDomainEvent evt,
        string entityName,
        Guid entityId,
        string orderNumber,
        MaterialMasterEntity material,
        decimal quantity,
        CancellationToken ct)
    {
        try
        {
            await audit.LogAsync(new AuditEntryDto
            {
                TenantId = evt.TenantId,
                UserId = Guid.TryParse(evt.SourceUserId, out var userId) ? userId : null,
                ModuleName = "MRP",
                ActionType = ActionType.Create,
                EntityName = entityName,
                EntityId = entityId.ToString(),
                Details = $"Event driven MRP draft {orderNumber} for {material.Code} quantity {quantity}"
            });
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Audit logging failed for event driven MRP draft {OrderNumber}", orderNumber);
        }
    }

    public void Dispose()
    {
        try
        {
            _drainCts.Cancel();
            _queue.Writer.TryComplete();
        }
        catch
        {
        }
        _drainCts.Dispose();
        _drainGate.Dispose();
    }
}
