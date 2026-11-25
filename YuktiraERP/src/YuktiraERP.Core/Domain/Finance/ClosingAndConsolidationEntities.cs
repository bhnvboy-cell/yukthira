using System;
using System.Collections.Generic;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Domain.Finance;

public enum FiscalPeriodStatus
{
    Open = 1,
    SoftLocked = 2, // No standard postings; only period-end adjustments
    HardLocked = 3  // Completely immutable; no postings allowed
}

public class FiscalCalendarPeriod : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public int FiscalYear { get; set; }
    public int PeriodNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public FiscalPeriodStatus Status { get; set; } = FiscalPeriodStatus.Open;
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedByUserId { get; set; }

    public void AssertOpenForPosting(bool isAdjustmentEntry = false)
    {
        if (Status == FiscalPeriodStatus.HardLocked)
            throw new InvalidOperationException($"Fiscal Period {FiscalYear}-{PeriodNumber:D2} is Hard Locked. All ledger postings are prohibited.");

        if (Status == FiscalPeriodStatus.SoftLocked && !isAdjustmentEntry)
            throw new InvalidOperationException($"Fiscal Period {FiscalYear}-{PeriodNumber:D2} is Soft Locked. Standard transactions cannot be posted; only adjustments are allowed.");
    }
}

public enum ChecklistItemStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Skipped = 4
}

public class PeriodCloseChecklistTask : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FiscalPeriodId { get; set; }
    public int StepSequence { get; set; }
    public string StepCode { get; set; } = string.Empty; // e.g. "REVAL_FX", "DEPR_RUN", "AP_ACCRUAL", "INTERCO_RECON", "GL_BALANCE_VERIFY"
    public string Title { get; set; } = string.Empty;
    public ChecklistItemStatus Status { get; set; } = ChecklistItemStatus.Pending;
    public bool IsMandatory { get; set; } = true;
    public string? CompletedBy { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ExecutionSummary { get; set; }
}

public class FxRevaluationResult
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ForeignCurrencyBalance { get; set; }
    public decimal BookValueFunctional { get; set; }
    public decimal ClosingRate { get; set; }
    public decimal RevaluedValueFunctional { get; set; }
    public decimal UnrealizedGainOrLoss => RevaluedValueFunctional - BookValueFunctional;
}

public class IntercompanyEliminationItem
{
    public Guid SourceCompanyId { get; set; }
    public Guid TargetCompanyId { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid TargetAccountId { get; set; }
    public decimal TransactionAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}
