using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class JournalPostRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
        public DateTime PostingDate { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string HeaderText { get; set; } = string.Empty;
        public List<JournalLineItemRequest> LineItems { get; set; } = new();
    }

    public class JournalLineItemRequest
    {
        public string GlAccount { get; set; } = string.Empty;
        public string CostCenter { get; set; } = string.Empty;
        public string ProfitCenter { get; set; } = string.Empty;
        public string Segment { get; set; } = string.Empty;
        public string Plant { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Assignment { get; set; } = string.Empty;
        public string ValueDate { get; set; } = string.Empty;
        public Dictionary<string, string>? ExtensionFields { get; set; }
    }

    public class JournalPostResult
    {
        public bool Success { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class JournalBatchPostRequest
    {
        public List<JournalPostRequest> Journals { get; set; } = new();
        public bool ValidateOnly { get; set; } = false;
    }

    public class JournalBatchPostResult
    {
        public bool AllSucceeded { get; set; }
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<JournalPostResult> Results { get; set; } = new();
        public List<JournalBatchError> Errors { get; set; } = new();
    }

    public class JournalBatchError
    {
        public int Index { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
    }

    public class JournalReverseRequest
    {
        public string DocumentNumber { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public DateTime PostingDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool ReverseOriginal { get; set; } = true;
    }

    public class JournalReverseResult
    {
        public bool Success { get; set; }
        public string ReversalDocumentNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class TrialBalanceRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string? GlAccountFrom { get; set; }
        public string? GlAccountTo { get; set; }
        public string? CostCenter { get; set; }
        public bool IncludeZeroBalances { get; set; } = false;
    }

    public class TrialBalanceResult
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public List<TrialBalanceLineItem> LineItems { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalBalance { get; set; }
        public bool IsBalanced { get; set; }
    }

    public class TrialBalanceLineItem
    {
        public string GlAccount { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal ClosingBalance { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    public class ProfitAndLossRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int PeriodFrom { get; set; }
        public int PeriodTo { get; set; }
        public string? ProfitCenter { get; set; }
        public string? Segment { get; set; }
        public bool ComparativeMode { get; set; } = false;
        public string? ComparativeFiscalYear { get; set; }
    }

    public class ProfitAndLossResult
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public List<PnLCategory> Categories { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetIncome { get; set; }
        public List<PnLCategory>? ComparativeCategories { get; set; }
    }

    public class PnLCategory
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal? ComparativeAmount { get; set; }
        public decimal PercentageChange { get; set; }
        public List<PnLLineItem> LineItems { get; set; } = new();
    }

    public class PnLLineItem
    {
        public string GlAccount { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class BalanceSheetRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string? ProfitCenter { get; set; }
        public string? Segment { get; set; }
        public bool ComparativeMode { get; set; } = false;
        public string? ComparativeFiscalYear { get; set; }
    }

    public class BalanceSheetResult
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public List<BSLineItem> Assets { get; set; } = new();
        public List<BSLineItem> Liabilities { get; set; } = new();
        public List<BSLineItem> Equity { get; set; } = new();
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public bool IsBalanced { get; set; }
    }

    public class BSLineItem
    {
        public string GlAccount { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal? ComparativeBalance { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int HierarchyLevel { get; set; }
    }

    public class CostCenterReportRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int PeriodFrom { get; set; }
        public int PeriodTo { get; set; }
        public string? CostCenterFrom { get; set; }
        public string? CostCenterTo { get; set; }
        public string? CostCenterCategory { get; set; }
        public bool IncludeSubCostCenters { get; set; } = true;
    }

    public class CostCenterReportResult
    {
        public string CompanyCode { get; set; } = string.Empty;
        public List<CostCenterReportLineItem> LineItems { get; set; } = new();
        public decimal TotalPlanned { get; set; }
        public decimal TotalActual { get; set; }
        public decimal TotalVariance { get; set; }
    }

    public class CostCenterReportLineItem
    {
        public string CostCenter { get; set; } = string.Empty;
        public string CostCenterName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal PlannedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Variance { get; set; }
        public decimal VariancePercentage { get; set; }
    }

    public class ProfitCenterReportRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int PeriodFrom { get; set; }
        public int PeriodTo { get; set; }
        public string? ProfitCenterFrom { get; set; }
        public string? ProfitCenterTo { get; set; }
        public bool IncludeSubProfitCenters { get; set; } = true;
    }

    public class ProfitCenterReportResult
    {
        public string CompanyCode { get; set; } = string.Empty;
        public List<ProfitCenterReportLineItem> LineItems { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalProfit { get; set; }
    }

    public class ProfitCenterReportLineItem
    {
        public string ProfitCenter { get; set; } = string.Empty;
        public string ProfitCenterName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal Profit { get; set; }
        public decimal Margin { get; set; }
    }

    public class JournalSimulateRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public DateTime PostingDate { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public List<JournalLineItemRequest> LineItems { get; set; } = new();
    }

    public class JournalSimulateResult
    {
        public bool IsValid { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal BalanceDifference { get; set; }
        public List<SimulatedLineItem> SimulatedLines { get; set; } = new();
    }

    public class SimulatedLineItem
    {
        public string GlAccount { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    public class JournalValidateRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public DateTime PostingDate { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public List<JournalLineItemRequest> LineItems { get; set; } = new();
        public bool CheckCompleteness { get; set; } = true;
        public bool CheckAuthorization { get; set; } = true;
        public bool CheckBudget { get; set; } = false;
    }

    public class JournalValidateResult
    {
        public bool IsValid { get; set; }
        public List<ValidationMessage> Messages { get; set; } = new();
    }

    public class ValidationMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? LineNumber { get; set; }
    }

    public interface IUniversalJournalService
    {
        Task<JournalPostResult> PostAsync(JournalPostRequest request);
        Task<JournalBatchPostResult> PostBatchAsync(JournalBatchPostRequest request);
        Task<JournalReverseResult> ReverseAsync(JournalReverseRequest request);
        Task<TrialBalanceResult> GetTrialBalanceAsync(TrialBalanceRequest request);
        Task<ProfitAndLossResult> GetProfitAndLossAsync(ProfitAndLossRequest request);
        Task<BalanceSheetResult> GetBalanceSheetAsync(BalanceSheetRequest request);
        Task<CostCenterReportResult> GetCostCenterReportAsync(CostCenterReportRequest request);
        Task<ProfitCenterReportResult> GetProfitCenterReportAsync(ProfitCenterReportRequest request);
        Task<JournalSimulateResult> SimulateAsync(JournalSimulateRequest request);
        Task<JournalValidateResult> ValidateAsync(JournalValidateRequest request);
    }
}
