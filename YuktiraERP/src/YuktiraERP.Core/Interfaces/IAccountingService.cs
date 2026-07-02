namespace YuktiraERP.Core.Interfaces;

public class JournalPostingRequest
{
    public string DocumentNumber { get; set; } = "";
    public DateTime EntryDate { get; set; } = DateTime.Today;
    public string Reference { get; set; } = "";
    public string Description { get; set; } = "";
    public List<JournalLine> Lines { get; set; } = new();
}

public class JournalLine
{
    public string AccountCode { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class TrialBalanceDto
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal DebitBalance { get; set; }
    public decimal CreditBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
}

public interface IAccountingService
{
    Task PostJournalEntryAsync(JournalPostingRequest request);
    Task<List<TrialBalanceDto>> GetTrialBalanceAsync(DateTime? asOfDate = null);
    Task<List<TrialBalanceDto>> GetProfitAndLossAsync(DateTime fromDate, DateTime toDate);
    Task<List<TrialBalanceDto>> GetBalanceSheetAsync(DateTime asOfDate);
}
