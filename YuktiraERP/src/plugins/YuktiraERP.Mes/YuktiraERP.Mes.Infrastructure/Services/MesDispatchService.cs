using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesDispatchService
{
    private readonly MesDbContext _db;

    public MesDispatchService(MesDbContext db) => _db = db;

    public async Task<MesWorkOrder> CreateWorkOrderAsync(
        Guid tenantId, string plantCode, string woNumber, Guid prodOrderId,
        Guid materialId, decimal plannedQty, DateTime dueDate, int priority = 100, CancellationToken ct = default)
    {
        var wo = new MesWorkOrder
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            WorkOrderNumber = woNumber,
            ProdOrderId = prodOrderId,
            MaterialId = materialId,
            PlannedQty = plannedQty,
            DueDate = dueDate,
            Priority = priority,
            Status = WorkOrderStatus.RELEASED
        };
        _db.WorkOrders.Add(wo);
        await _db.SaveChangesAsync(ct);
        return wo;
    }

    public async Task<MesWoOperation> AddOperationAsync(
        Guid tenantId, string plantCode, Guid woId, int opNo, Guid workCenterId,
        Guid? resourceId, decimal qtyPlanned, double stdSetupMin, double stdRunMin,
        bool ready = false, CancellationToken ct = default)
    {
        var op = new MesWoOperation
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            WoId = woId,
            OperationNo = opNo,
            WorkCenterId = workCenterId,
            ResourceId = resourceId,
            QtyPlanned = qtyPlanned,
            StdSetupMin = stdSetupMin,
            StdRunMin = stdRunMin,
            Status = ready ? WoOperationStatus.READY : WoOperationStatus.QUEUED
        };
        _db.WoOperations.Add(op);

        if (resourceId.HasValue)
        {
            var nextSeq = await _db.DispatchQueues
                .Where(q => q.TenantId == tenantId && q.ResourceId == resourceId.Value)
                .Select(q => (int?)q.Seq).MaxAsync(ct) ?? 0;

            _db.DispatchQueues.Add(new MesDispatchQueue
            {
                TenantId = tenantId,
                PlantCode = plantCode,
                ResourceId = resourceId.Value,
                Seq = nextSeq + 1,
                WoOpId = op.Id
            });
        }

        await _db.SaveChangesAsync(ct);
        return op;
    }

    public async Task<IReadOnlyList<MesDispatchQueue>> GetDispatchQueueAsync(
        Guid tenantId, Guid? resourceId = null, CancellationToken ct = default)
    {
        var q = _db.DispatchQueues.Where(x => x.TenantId == tenantId);
        if (resourceId.HasValue)
            q = q.Where(x => x.ResourceId == resourceId.Value);
        return await q.OrderBy(x => x.Seq).ToListAsync(ct);
    }

    public async Task<MesWoOperation> StartOperationAsync(
        Guid tenantId, Guid woOpId, string operatorUser, bool supervisorOverride = false, CancellationToken ct = default)
    {
        var op = await _db.WoOperations.FirstOrDefaultAsync(o => o.Id == woOpId && o.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Operation {woOpId} not found.");

        if (op.Status != WoOperationStatus.READY && op.Status != WoOperationStatus.PAUSED && !supervisorOverride)
            throw new InvalidOperationException($"Operation {op.OperationNo} is in state {op.Status}, not READY.");

        op.Status = WoOperationStatus.RUNNING;
        op.RowVersion++;

        var evt = new MesDispatchEvent
        {
            TenantId = tenantId,
            PlantCode = op.PlantCode,
            WoOpId = op.Id,
            EventType = DispatchEventType.DISPATCH,
            ToResourceId = op.ResourceId,
            Reason = supervisorOverride ? "Supervisor override start" : "Operator start",
            ByUser = operatorUser
        };
        evt.RecordHash = RecordHashHelper.ComputeSha256(evt.Id, evt.WoOpId, evt.EventType, evt.ByUser, evt.CreatedAt);
        _db.DispatchEvents.Add(evt);

        await _db.SaveChangesAsync(ct);
        return op;
    }

    /// <summary>
    /// Idempotent floor confirmation. Writes MesOpConfirmation, MesOutboxEvent, and MesErpPostingLog
    /// in the same DB transaction; ERP posting happens asynchronously so the floor is never blocked.
    /// </summary>
    public async Task<MesOpConfirmation> ConfirmOperationAsync(
        Guid tenantId,
        Guid woOpId,
        Guid idempotencyKey,
        decimal goodQty,
        decimal scrapQty,
        decimal reworkQty,
        string? scrapReason,
        double laborMin,
        double machineMin,
        string actor,
        CancellationToken ct = default)
    {
        var existing = await _db.OpConfirmations
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.IdempotencyKey == idempotencyKey, ct);
        if (existing != null)
            return existing;

        var op = await _db.WoOperations.FirstOrDefaultAsync(o => o.Id == woOpId && o.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Operation {woOpId} not found.");

        op.QtyGood += goodQty;
        op.QtyScrap += scrapQty;
        op.QtyRework += reworkQty;
        if (op.QtyGood + op.QtyScrap >= op.QtyPlanned)
        {
            op.Status = WoOperationStatus.COMPLETE;
        }
        op.RowVersion++;

        var confirmation = new MesOpConfirmation
        {
            TenantId = tenantId,
            PlantCode = op.PlantCode,
            IdempotencyKey = idempotencyKey,
            WoOpId = woOpId,
            GoodQty = goodQty,
            ScrapQty = scrapQty,
            ReworkQty = reworkQty,
            ScrapReason = scrapReason,
            LaborMin = laborMin,
            MachineMin = machineMin,
            ConfirmedAt = DateTime.UtcNow,
            PostingStatus = PostingStatus.PENDING,
            CreatedBy = actor
        };
        _db.OpConfirmations.Add(confirmation);

        // Transactional outbox + async ERP posting log
        _db.OutboxEvents.Add(new MesOutboxEvent
        {
            TenantId = tenantId,
            PlantCode = op.PlantCode,
            EventType = "operation.confirmed",
            AggregateType = nameof(MesOpConfirmation),
            AggregateId = confirmation.Id,
            PayloadJson = JsonSerializer.Serialize(new
            {
                confirmation.Id,
                confirmation.WoOpId,
                confirmation.GoodQty,
                confirmation.ScrapQty,
                confirmation.LaborMin,
                confirmation.MachineMin
            })
        });

        _db.ErpPostingLogs.Add(new MesErpPostingLog
        {
            TenantId = tenantId,
            PlantCode = op.PlantCode,
            TargetModule = "PP",
            PostingType = "ProductionConfirmation",
            SourceEntityId = confirmation.Id,
            IdempotencyKey = idempotencyKey,
            PostingStatus = PostingStatus.PENDING
        });

        // Auto-promote next operation in the same work order if complete
        if (op.Status == WoOperationStatus.COMPLETE)
        {
            var nextOp = await _db.WoOperations
                .Where(o => o.TenantId == tenantId && o.WoId == op.WoId && o.OperationNo > op.OperationNo && o.Status == WoOperationStatus.QUEUED)
                .OrderBy(o => o.OperationNo)
                .FirstOrDefaultAsync(ct);

            if (nextOp != null)
                nextOp.Status = WoOperationStatus.READY;
        }

        await _db.SaveChangesAsync(ct);
        return confirmation;
    }
}
