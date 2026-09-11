using YuktiraERP.Core.Enums;

namespace YuktiraERP.Core.Dtos;

public class OfflineTransactionDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = string.Empty;
    public OfflineTransactionType TransactionType { get; set; }
    public OfflineTransactionStatus Status { get; set; } = OfflineTransactionStatus.Queued;
    public DateTime ClientTimestamp { get; set; }
    public DateTime? ServerTimestamp { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public int RetryCount { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = new();
}

public class OfflineQueueProcessResult
{
    public bool Success { get; set; }
    public int TotalProcessed { get; set; }
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public int ConflictsDetected { get; set; }
    public List<OfflineTransactionResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class OfflineTransactionResult
{
    public Guid TransactionId { get; set; }
    public OfflineTransactionStatus Status { get; set; }
    public string? ServerDocumentId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class OfflineConflictDto
{
    public Guid TransactionId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string ClientVersion { get; set; } = string.Empty;
    public string ServerVersion { get; set; } = string.Empty;
    public ConflictResolutionStrategy Strategy { get; set; }
    public string? Resolution { get; set; }
}

public class OfflineConflictResult
{
    public bool Success { get; set; }
    public ConflictResolutionStrategy AppliedStrategy { get; set; }
    public string? MergedPayload { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class LowStockNotificationDto
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinimumQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class ProductionHoldNotificationDto
{
    public string ProductionOrderNumber { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string HoldStatus { get; set; } = string.Empty;
}

public class QualityAlertDto
{
    public string InspectionLotNumber { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string DefectType { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public class GenericAlertDto
{
    public MobileAlertType AlertType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public Dictionary<string, string> Data { get; set; } = new();
}
