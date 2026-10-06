namespace YuktiraERP.Core.Interfaces;

public class MrpDomainEvent
{
    public string EventType { get; set; } = "";
    public Guid TenantId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public List<string> MaterialCodes { get; set; } = new();
    public Dictionary<string, decimal>? Quantities { get; set; }
    public string ReferenceId { get; set; } = "";
    public string? SourceUserId { get; set; }
}

public class MrpShortage
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal RequiredQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public string ProcurementType { get; set; } = "PURCHASE";
    public string ActionTaken { get; set; } = "";
    public string ReferenceId { get; set; } = "";
}

public class MrpEventTotals
{
    public int EvaluatedMaterials { get; set; }
    public int ShortageCount { get; set; }
    public decimal TotalShortageQuantity { get; set; }
    public int DraftPurchaseOrders { get; set; }
    public int DraftProductionOrders { get; set; }
    public int SkippedMaterials { get; set; }
}

public class MrpEventResult
{
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public Guid TenantId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public int EvaluatedMaterials { get; set; }
    public List<MrpShortage> Shortages { get; set; } = new();
    public List<string> DraftPurchaseOrders { get; set; } = new();
    public List<string> DraftProductionOrders { get; set; } = new();
    public List<string> SkippedReasons { get; set; } = new();
    public MrpEventTotals Totals { get; set; } = new();
}

public class MrpEngineEventSummary
{
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public Guid TenantId { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime ProcessedAt { get; set; }
    public int EvaluatedMaterials { get; set; }
    public int DraftPurchaseOrders { get; set; }
    public int DraftProductionOrders { get; set; }
    public int SkippedCount { get; set; }
}

public class MrpEngineStats
{
    public bool Enabled { get; set; }
    public int RegisteredListeners { get; set; }
    public long EventsHandled { get; set; }
    public long DraftPurchaseOrdersCreated { get; set; }
    public long DraftProductionOrdersCreated { get; set; }
    public long ShortagesDetected { get; set; }
    public long QueuedEvents { get; set; }
    public bool DrainLoopRunning { get; set; }
    public DateTime? LastEventAt { get; set; }
    public List<MrpEngineEventSummary> RecentEvents { get; set; } = new();
}

public class MrpEngineOptions
{
    public const string SectionName = "EventDrivenMrp";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 10;
    public int MaxRecentEvents { get; set; } = 20;
    public int DefaultLeadTimeDays { get; set; } = 7;
}

public interface IMrpEventListener
{
    Task<MrpEventResult> HandleAsync(MrpDomainEvent evt, CancellationToken ct = default);
}
