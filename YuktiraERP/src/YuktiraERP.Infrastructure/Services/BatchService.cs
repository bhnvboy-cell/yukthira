using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Caching;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class BatchService : IBatchService
{
    private readonly YuktiraDbContext _db;

    public BatchService(YuktiraDbContext db) => _db = db;

    public async Task<BatchEntity> CreateBatchAsync(BatchEntity batch)
    {
        batch.Id = Guid.NewGuid();
        batch.CreatedAt = DateTime.UtcNow;
        batch.Status = "ACTIVE";
        batch.QuantityConsumed = 0;
        _db.Batches.Add(batch);
        await _db.SaveChangesAsync();
        return batch;
    }

    public async Task<BatchEntity?> GetBatchAsync(Guid id)
        => await _db.Batches.FindAsync(id);

    public async Task<BatchEntity?> GetBatchByNumberAsync(string batchNumber)
        => await _db.Batches.FirstOrDefaultAsync(b => b.BatchNumber == batchNumber);

    public async Task<List<BatchEntity>> GetAllBatchesAsync()
        => await _db.Batches.AsNoTracking().ToListAsync();

    public async Task<List<BatchEntity>> GetBatchesByMaterialAsync(Guid materialId)
        => await _db.Batches.AsNoTracking().Where(b => b.MaterialId == materialId).ToListAsync();

    public async Task<BatchEntity> UpdateBatchAsync(BatchEntity batch)
    {
        batch.UpdatedAt = DateTime.UtcNow;
        _db.Batches.Update(batch);
        await _db.SaveChangesAsync();
        return batch;
    }

    public async Task ExpireBatchAsync(Guid batchId)
    {
        var batch = await _db.Batches.FindAsync(batchId);
        if (batch != null && batch.Status == "ACTIVE")
        {
            batch.Status = "EXPIRED";
            batch.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<RecallEntity> RecallBatchAsync(string recallNumber, List<Guid> batchIds, string reason, Guid initiatedBy)
    {
        var batchNumbers = new List<string>();
        foreach (var batchId in batchIds)
        {
            var batch = await _db.Batches.FindAsync(batchId);
            if (batch != null)
            {
                batch.Status = "RECALLED";
                batch.UpdatedAt = DateTime.UtcNow;
                batchNumbers.Add(batch.BatchNumber);
            }
        }

        var recall = new RecallEntity
        {
            Id = Guid.NewGuid(),
            RecallNumber = recallNumber,
            Reason = reason,
            AffectedBatchIds = JsonSerializer.Serialize(batchIds),
            AffectedBatchNumbers = string.Join(", ", batchNumbers),
            InitiatedBy = initiatedBy,
            InitiatedDate = DateTime.UtcNow,
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };

        _db.Recalls.Add(recall);
        await _db.SaveChangesAsync();
        return recall;
    }

    public async Task<SerialNumberEntity> CreateSerialNumberAsync(SerialNumberEntity serial)
    {
        serial.Id = Guid.NewGuid();
        serial.CreatedAt = DateTime.UtcNow;
        serial.Status = "ACTIVE";
        _db.SerialNumbers.Add(serial);
        await _db.SaveChangesAsync();
        return serial;
    }

    public async Task<SerialNumberEntity?> GetSerialNumberAsync(Guid id)
        => await _db.SerialNumbers.FindAsync(id);

    public async Task<SerialNumberEntity?> GetSerialNumberByNumberAsync(string serialNumber)
        => await _db.SerialNumbers.FirstOrDefaultAsync(s => s.SerialNumber == serialNumber);

    public async Task<List<SerialNumberEntity>> GetSerialNumbersByBatchAsync(Guid batchId)
        => await _db.SerialNumbers.AsNoTracking().Where(s => s.BatchId == batchId).ToListAsync();

    public async Task<List<SerialNumberEntity>> GetSerialHistoryAsync(string serialNumber)
        => await _db.SerialNumbers.AsNoTracking().Where(s => s.SerialNumber == serialNumber).ToListAsync();

    public async Task<BatchMovementEntity> RecordMovementAsync(BatchMovementEntity movement)
    {
        movement.Id = Guid.NewGuid();
        movement.CreatedAt = DateTime.UtcNow;
        movement.MovementDate = DateTime.UtcNow;

        _db.BatchMovements.Add(movement);

        var batch = await _db.Batches.FindAsync(movement.BatchId);
        if (batch != null)
        {
            if (movement.MovementType == "RECEIPT" || movement.MovementType == "ADJUSTMENT")
                batch.Quantity += movement.Quantity;
            else if (movement.MovementType == "ISSUE")
            {
                batch.Quantity -= movement.Quantity;
                batch.QuantityConsumed += movement.Quantity;
            }
            batch.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return movement;
    }

    public async Task<List<BatchMovementEntity>> GetBatchHistoryAsync(Guid batchId)
        => await _db.BatchMovements.AsNoTracking()
            .Where(m => m.BatchId == batchId)
            .OrderByDescending(m => m.MovementDate)
            .ToListAsync();

    public async Task<BatchTraceabilityResult> GetBatchTraceabilityAsync(Guid batchId)
    {
        var batch = await _db.Batches.FindAsync(batchId);
        var movements = await GetBatchHistoryAsync(batchId);
        var serials = await GetSerialNumbersByBatchAsync(batchId);

        return new BatchTraceabilityResult
        {
            Batch = batch,
            ForwardTrace = movements.Where(m => m.MovementType == "RECEIPT" || m.MovementType == "TRANSFER").ToList(),
            BackwardTrace = movements.Where(m => m.MovementType == "ISSUE" || m.MovementType == "TRANSFER").ToList(),
            SerialNumbers = serials
        };
    }

    public async Task<List<BatchEntity>> CheckExpiryAsync()
    {
        var expiredBatches = await _db.Batches
            .Where(b => b.Status == "ACTIVE" && b.ExpiryDate != null && b.ExpiryDate <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var batch in expiredBatches)
        {
            batch.Status = "EXPIRED";
            batch.UpdatedAt = DateTime.UtcNow;
        }

        if (expiredBatches.Any())
            await _db.SaveChangesAsync();

        return expiredBatches;
    }

    public async Task<string> GenerateBatchCertificateAsync(Guid batchId)
    {
        var batch = await _db.Batches.FindAsync(batchId);
        if (batch == null) return "";

        var certificate = $"CERTIFICATE OF ANALYSIS\n" +
            $"Batch Number: {batch.BatchNumber}\n" +
            $"Material: {batch.MaterialName}\n" +
            $"Manufacturing Date: {batch.ManufacturingDate:yyyy-MM-dd}\n" +
            $"Expiry Date: {batch.ExpiryDate?.ToString("yyyy-MM-dd") ?? "N/A"}\n" +
            $"Quantity: {batch.Quantity} {batch.UnitOfMeasure}\n" +
            $"Supplier: {batch.SupplierName}\n" +
            $"Status: {batch.Status}\n" +
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        batch.CertificateOfAnalysis = certificate;
        batch.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return certificate;
    }

    public async Task<List<RecallEntity>> GetRecallsAsync()
        => await _db.Recalls.AsNoTracking().OrderByDescending(r => r.InitiatedDate).ToListAsync();

    public async Task<RecallEntity> UpdateRecallStatusAsync(Guid recallId, string status, string resolutionNotes)
    {
        var recall = await _db.Recalls.FindAsync(recallId);
        if (recall != null)
        {
            recall.Status = status;
            recall.ResolutionNotes = resolutionNotes;
            if (status == "CLOSED")
                recall.ResolvedDate = DateTime.UtcNow;
            recall.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return recall!;
    }
}
