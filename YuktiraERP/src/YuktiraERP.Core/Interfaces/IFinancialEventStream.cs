namespace YuktiraERP.Core.Interfaces;

public class FinancialEventInput
{
    public Guid TenantId { get; set; }
    public Guid StreamId { get; set; }
    public string StreamType { get; set; } = "UniversalJournal";
    public string EventType { get; set; } = "";
    public string Payload { get; set; } = "{}";
    public string CorrelationId { get; set; } = "";
}

public class FinancialEventDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StreamId { get; set; }
    public string StreamType { get; set; } = "UniversalJournal";
    public long Sequence { get; set; }
    public string EventType { get; set; } = "";
    public string Payload { get; set; } = "{}";
    public string CorrelationId { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string PreviousHash { get; set; } = "";
    public string Hash { get; set; } = "";
    public DateTime? AppliedAt { get; set; }
    public string Error { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class FinancialEventEnvelope
{
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
    public Guid StreamId { get; set; }
    public string StreamType { get; set; } = "";
    public long Sequence { get; set; }
    public string EventType { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string PreviousHash { get; set; } = "";
    public string Hash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string CorrelationId { get; set; } = "";
}

public class StreamReplayLine
{
    public string AccountCode { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string Currency { get; set; } = "";
    public string Description { get; set; } = "";
    public string Reference { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class StreamReplayResult
{
    public Guid StreamId { get; set; }
    public int EventCount { get; set; }
    public int AppliedEventCount { get; set; }
    public List<StreamReplayLine> Lines { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public bool IsBalanced { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class FinancialEventStreamOptions
{
    public const string SectionName = "FinancialEventStream";
    public const string GenesisHash = "GENESIS";
    public const string JournalPostedEvent = "JournalPosted";
    public const string JournalBatchPostedEvent = "JournalBatchPosted";

    public bool Enabled { get; set; } = true;
    public int FlushMilliseconds { get; set; } = 500;
    public int BatchSize { get; set; } = 100;
}

public interface IFinancialEventStream
{
    int ChannelDepth { get; }

    Task<FinancialEventEnvelope> EnqueueAsync(FinancialEventInput input, CancellationToken ct = default);

    Task<int> DrainPendingAsync(CancellationToken ct = default);

    Task<IReadOnlyList<FinancialEventDto>> GetStreamAsync(Guid streamId, Guid tenantId);

    Task<StreamReplayResult> ReplayAsync(Guid streamId, Guid tenantId, CancellationToken ct = default);
}
