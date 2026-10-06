namespace YuktiraERP.Core.Interfaces;

public class SelfHealingOptions
{
    public const string SectionName = "SelfHealing";

    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
    public int StartupDelaySeconds { get; set; } = 30;
    public decimal PennyTolerance { get; set; } = 0.05m;
    public decimal MaxAutoFixAmount { get; set; } = 1000m;
    public int StuckPostingAgeMinutes { get; set; } = 60;
}

public class SelfHealingActionDto
{
    public string Category { get; set; } = "";
    public string TargetEntity { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string Summary { get; set; } = "";
    public decimal DeltaAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = "";
    public string UserId { get; set; } = "system";
    public string Details { get; set; } = "{}";
    public long SequenceNumber { get; set; }
    public string PreviousHash { get; set; } = "";
    public string CurrentHash { get; set; } = "";
}

public class SelfHealingRunResultDto
{
    public Guid TenantId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public int GrIrScanned { get; set; }
    public int PennyScanned { get; set; }
    public int StuckPostingsScanned { get; set; }
    public int AppliedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FlaggedCount { get; set; }
    public int FailedCount { get; set; }
    public List<SelfHealingActionDto> Actions { get; set; } = new();
}

public interface ISelfHealingReconciliationService
{
    Task<SelfHealingRunResultDto> RunOnceAsync(Guid tenantId, string userId, CancellationToken cancellationToken = default);
}
