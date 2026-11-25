using System;
using System.Collections.Generic;
using System.Linq;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Domain.Finance;

public enum AccountCategory
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public enum AccountNormalBalance
{
    Debit = 1,
    Credit = 2
}

public class ChartOfAccount : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string AccountCode { get; set; } = string.Empty; // e.g. "101000", "201000", "401000"
    public string AccountName { get; set; } = string.Empty;
    public AccountCategory Category { get; set; }
    public AccountNormalBalance NormalBalance { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsReconciliationAccount { get; set; } = false; // AP/AR sub-ledger control
    public bool IsIntercompanyAccount { get; set; } = false;
    public bool AllowsManualJournal { get; set; } = true;
    public string CurrencyCode { get; set; } = "USD";
    public bool IsActive { get; set; } = true;

    public ChartOfAccount? ParentAccount { get; set; }
    public ICollection<ChartOfAccount> SubAccounts { get; set; } = new List<ChartOfAccount>();
}

public enum JournalEntryType
{
    Standard = 1,
    Reversal = 2,
    Closing = 3,
    Revaluation = 4,
    IntercompanyElimination = 5
}

/// <summary>
/// Immutable, double-entry financial ledger entry.
/// Partitioned by fiscal period (finance.general_ledger_entries).
/// Invariant: TotalDebit == TotalCredit across all lines.
/// </summary>
public class JournalEntry : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public DateOnly PostingDate { get; set; }
    public DateOnly DocumentDate { get; set; }
    public int FiscalYear { get; set; }
    public int FiscalPeriod { get; set; } // 1..12 or 13 for closing adjustments
    public JournalEntryType EntryType { get; set; } = JournalEntryType.Standard;
    public GenericDocumentState Status { get; set; } = GenericDocumentState.Draft;
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public string Description { get; set; } = string.Empty;
    public string? SourceDocumentType { get; set; } // e.g. "SalesInvoice", "ApInvoice", "GoodsReceiptPo"
    public Guid? SourceDocumentId { get; set; }
    public Guid? ReversalOfEntryId { get; set; }
    public bool IsReversed { get; set; } = false;

    public decimal TotalDebit => Lines.Sum(l => l.DebitAmount);
    public decimal TotalCredit => Lines.Sum(l => l.CreditAmount);

    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();

    public void AssertDoubleEntryBalance()
    {
        if (Lines == null || Lines.Count < 2)
            throw new InvalidOperationException($"Journal entry {EntryNumber} must contain at least two lines for double-entry accounting.");

        decimal debits = TotalDebit;
        decimal credits = TotalCredit;

        if (Math.Abs(debits - credits) > 0.0001m)
        {
            throw new InvalidOperationException(
                $"Double-entry invariant violated for entry {EntryNumber}. Total Debits ({debits:F2}) must equal Total Credits ({credits:F2}). Discrepancy: {Math.Abs(debits - credits):F4}");
        }
    }

    public void AssertCanMutate()
    {
        if (Status == GenericDocumentState.Posted || Status == GenericDocumentState.Closed || Status == GenericDocumentState.Cancelled || Status == GenericDocumentState.Reversed)
            throw new InvalidOperationException($"Journal entry {EntryNumber} in status '{Status}' is immutable. Corrections must be made via reversal.");
    }
}

public class JournalEntryLine : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JournalEntryId { get; set; }
    public int LineNumber { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; } = 0m;
    public decimal CreditAmount { get; set; } = 0m;
    public decimal FunctionalDebitAmount { get; set; } = 0m;
    public decimal FunctionalCreditAmount { get; set; } = 0m;
    public Guid? CostCenterId { get; set; }
    public Guid? ProfitCenterId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? BusinessPartnerId { get; set; }
    public string? LineDescription { get; set; }
}
