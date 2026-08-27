using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Caching;

public interface IBatchService
{
    Task<BatchEntity> CreateBatchAsync(BatchEntity batch);
    Task<BatchEntity?> GetBatchAsync(Guid id);
    Task<BatchEntity?> GetBatchByNumberAsync(string batchNumber);
    Task<List<BatchEntity>> GetAllBatchesAsync();
    Task<List<BatchEntity>> GetBatchesByMaterialAsync(Guid materialId);
    Task<BatchEntity> UpdateBatchAsync(BatchEntity batch);
    Task ExpireBatchAsync(Guid batchId);
    Task<RecallEntity> RecallBatchAsync(string recallNumber, List<Guid> batchIds, string reason, Guid initiatedBy);

    Task<SerialNumberEntity> CreateSerialNumberAsync(SerialNumberEntity serial);
    Task<SerialNumberEntity?> GetSerialNumberAsync(Guid id);
    Task<SerialNumberEntity?> GetSerialNumberByNumberAsync(string serialNumber);
    Task<List<SerialNumberEntity>> GetSerialNumbersByBatchAsync(Guid batchId);
    Task<List<SerialNumberEntity>> GetSerialHistoryAsync(string serialNumber);

    Task<BatchMovementEntity> RecordMovementAsync(BatchMovementEntity movement);
    Task<List<BatchMovementEntity>> GetBatchHistoryAsync(Guid batchId);
    Task<BatchTraceabilityResult> GetBatchTraceabilityAsync(Guid batchId);

    Task<List<BatchEntity>> CheckExpiryAsync();
    Task<string> GenerateBatchCertificateAsync(Guid batchId);
    Task<List<RecallEntity>> GetRecallsAsync();
    Task<RecallEntity> UpdateRecallStatusAsync(Guid recallId, string status, string resolutionNotes);
}

public class BatchTraceabilityResult
{
    public BatchEntity? Batch { get; set; }
    public List<BatchMovementEntity> ForwardTrace { get; set; } = new();
    public List<BatchMovementEntity> BackwardTrace { get; set; } = new();
    public List<SerialNumberEntity> SerialNumbers { get; set; } = new();
}
