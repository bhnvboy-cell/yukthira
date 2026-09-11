namespace YuktiraERP.Core.Enums;

public enum OfflineTransactionType
{
    GoodsReceipt = 1,
    GoodsIssue = 2,
    StockTransfer = 3,
    PhysicalCount = 4,
    InspectionResult = 5,
    UsageDecision = 6,
    DeliveryConfirmation = 7,
    ReturnReceipt = 8
}

public enum OfflineTransactionStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    ConflictDetected = 4,
    ConflictResolved = 5
}

public enum ConflictResolutionStrategy
{
    ServerWins = 0,
    ClientWins = 1,
    Merge = 2,
    ManualReview = 3
}

public enum MobileAlertType
{
    LowStock = 1,
    ProductionHold = 2,
    QualityNonConformance = 3,
    DeliveryOverdue = 4,
    InspectionDue = 5,
    MaintenanceRequired = 6,
    Generic = 99
}
