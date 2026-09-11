namespace YuktiraERP.Core.Enums;

public enum EventType
{
    DomainEvent = 0,
    IntegrationEvent = 1,
    SystemEvent = 2
}

public enum AggregateType
{
    Material = 0,
    StockBalance = 1,
    PurchaseOrder = 2,
    SalesOrder = 3,
    GoodsReceipt = 4,
    GoodsIssue = 5,
    InspectionLot = 6,
    UsageDecision = 7,
    Vendor = 8,
    Customer = 9,
    JournalEntry = 10,
    ProductionOrder = 11
}

public enum ProjectionStatus
{
    Current = 0,
    Stale = 1,
    Rebuilding = 2,
    Failed = 3
}
