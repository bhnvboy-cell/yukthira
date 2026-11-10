namespace YuktiraERP.Core.Interfaces;

public class FxValuationItemDto
{
    public Guid JournalId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string ForeignCurrency { get; set; } = string.Empty;
    public decimal ForeignAmount { get; set; }
    public decimal HistoricalRate { get; set; }
    public decimal BookValueLC { get; set; }
    public decimal ClosingRate { get; set; }
    public decimal RevaluedValueLC { get; set; }
    public decimal UnrealizedDeltaLC { get; set; }
    public string ValuationStatus { get; set; } = "Preview"; // "Gain", "Loss", "Neutral"
}

public class FxValuationRunResult
{
    public Guid RunId { get; set; } = Guid.NewGuid();
    public DateTime ValuationDate { get; set; }
    public string BaseCurrency { get; set; } = "INR";
    public int ItemsEvaluated { get; set; }
    public decimal TotalUnrealizedGain { get; set; }
    public decimal TotalUnrealizedLoss { get; set; }
    public decimal NetUnrealizedImpact { get; set; }
    public string? JournalDocumentNumber { get; set; }
    public bool EntriesPosted { get; set; }
    public List<FxValuationItemDto> Items { get; set; } = new();
}

public interface IFxValuationService
{
    Task<List<FxValuationItemDto>> PreviewRevaluationAsync(Guid tenantId, string baseCurrency, DateTime valuationDate);
    Task<FxValuationRunResult> ExecuteRevaluationAsync(Guid tenantId, string baseCurrency, DateTime valuationDate, bool postEntries, string userId);
}
