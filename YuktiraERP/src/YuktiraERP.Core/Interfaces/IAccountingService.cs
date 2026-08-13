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

public class AgingBucketDto
{
    public string PartyName { get; set; } = "";
    public decimal Current { get; set; }
    public decimal Days1to30 { get; set; }
    public decimal Days31to60 { get; set; }
    public decimal Days61to90 { get; set; }
    public decimal Over90 { get; set; }
    public decimal Total { get; set; }
}

public class AgingSummaryDto
{
    public string Kind { get; set; } = "";
    public decimal TotalOutstanding { get; set; }
    public decimal Current { get; set; }
    public decimal Days1to30 { get; set; }
    public decimal Days31to60 { get; set; }
    public decimal Days61to90 { get; set; }
    public decimal Over90 { get; set; }
    public List<AgingBucketDto> Parties { get; set; } = new();
}

public class PaymentRequest
{
    public string PartyName { get; set; } = "";
    public string Type { get; set; } = "Payment";
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = "";
    public string Method { get; set; } = "Bank Transfer";
    public string CashAccountCode { get; set; } = "";
}

public class PeriodCloseRequest
{
    public string Period { get; set; } = "";
    public string FiscalYear { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class BankReconciliationRequest
{
    public string AccountCode { get; set; } = "";
    public DateTime StatementDate { get; set; } = DateTime.Today;
    public decimal StatementBalance { get; set; }
    public decimal LedgerBalance { get; set; }
    public string Notes { get; set; } = "";
}

public class DepreciationRunRequest
{
    public string Period { get; set; } = "";
}

public class PaymentRecordDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = "";
    public DateTime Date { get; set; }
    public string PartyName { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public string Status { get; set; } = "";
}

public class FiscalPeriodDto
{
    public Guid Id { get; set; }
    public string Period { get; set; } = "";
    public string FiscalYear { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "";
    public DateTime? ClosedAt { get; set; }
}

public class BankReconciliationDto
{
    public Guid Id { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public DateTime StatementDate { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal LedgerBalance { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = "";
}

public class DepreciationScheduleDto
{
    public Guid Id { get; set; }
    public string AssetCode { get; set; } = "";
    public string AssetName { get; set; } = "";
    public string Period { get; set; } = "";
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue { get; set; }
    public string Status { get; set; } = "";
}

public interface IAccountingService
{
    Task PostJournalEntryAsync(JournalPostingRequest request);
    Task<List<TrialBalanceDto>> GetTrialBalanceAsync(Guid tenantId, DateTime? asOfDate = null);
    Task<List<TrialBalanceDto>> GetProfitAndLossAsync(Guid tenantId, DateTime fromDate, DateTime toDate);
    Task<List<TrialBalanceDto>> GetBalanceSheetAsync(Guid tenantId, DateTime asOfDate);

    // Finance loop: AP/AR aging
    Task<AgingSummaryDto> GetAccountsPayableAgingAsync(Guid tenantId, DateTime asOfDate);
    Task<AgingSummaryDto> GetAccountsReceivableAgingAsync(Guid tenantId, DateTime asOfDate);

    // Finance loop: payments
    Task PostPaymentAsync(Guid tenantId, PaymentRequest request);
    Task<List<PaymentRecordDto>> GetPaymentHistoryAsync(Guid tenantId, int limit = 50);

    // Finance loop: period close
    Task OpenPeriodAsync(Guid tenantId, PeriodCloseRequest request);
    Task ClosePeriodAsync(Guid tenantId, string period, string closedBy);
    Task<List<FiscalPeriodDto>> GetFiscalPeriodsAsync(Guid tenantId);

    // Finance loop: bank reconciliation
    Task PostBankReconciliationAsync(Guid tenantId, BankReconciliationRequest request);
    Task<List<BankReconciliationDto>> GetBankReconciliationsAsync(Guid tenantId, int limit = 50);

    // Finance loop: fixed asset depreciation
    Task RunDepreciationAsync(Guid tenantId, DepreciationRunRequest request);
    Task<List<DepreciationScheduleDto>> GetDepreciationScheduleAsync(Guid tenantId, string? period = null);
}