using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class ConsolidationGroupCreateRequest
    {
        public string GroupName { get; set; } = string.Empty;
        public string GroupDescription { get; set; } = string.Empty;
        public string GroupType { get; set; } = string.Empty;
        public string ReportingCurrency { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string ConsolidationMethod { get; set; } = string.Empty;
        public string? ParentGroupId { get; set; }
    }

    public class ConsolidationGroupCreateResult
    {
        public bool Success { get; set; }
        public string GroupId { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ConsolidationEntityAddRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string EntityCode { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string FunctionalCurrency { get; set; } = string.Empty;
        public decimal OwnershipPercentage { get; set; }
        public bool IsEliminationEntity { get; set; } = false;
        public string? ParentEntityId { get; set; }
    }

    public class ConsolidationEntityAddResult
    {
        public bool Success { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string EntityCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ConsolidationEntityDataSubmitRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public List<ConsolidationTrialBalanceLine> TrialBalance { get; set; } = new();
        public List<ConsolidationProfitAndLossLine> ProfitAndLoss { get; set; } = new();
        public List<ConsolidationCashFlowLine> CashFlows { get; set; } = new();
        public List<ConsolidationEquityMovementLine> EquityMovements { get; set; } = new();
    }

    public class ConsolidationTrialBalanceLine
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public decimal LocalCurrencyAmount { get; set; }
        public string LocalCurrency { get; set; } = string.Empty;
        public decimal? TranslatedAmount { get; set; }
    }

    public class ConsolidationProfitAndLossLine
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal LocalCurrencyAmount { get; set; }
        public string LocalCurrency { get; set; } = string.Empty;
    }

    public class ConsolidationCashFlowLine
    {
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal LocalCurrencyAmount { get; set; }
        public string LocalCurrency { get; set; } = string.Empty;
    }

    public class ConsolidationEquityMovementLine
    {
        public string EquityAccount { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal Movements { get; set; }
        public decimal ClosingBalance { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    public class ConsolidationEntityDataSubmitResult
    {
        public bool Success { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ConsolidationEntityDataValidateRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
    }

    public class ConsolidationEntityDataValidateResult
    {
        public bool IsValid { get; set; }
        public List<ConsolidationValidationMessage> Messages { get; set; } = new();
        public decimal TrialBalanceDifference { get; set; }
        public bool IsTrialBalanceBalanced { get; set; }
    }

    public class ConsolidationValidationMessage
    {
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string? AccountCode { get; set; }
    }

    public class CurrencyTranslationRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string TranslationMethod { get; set; } = string.Empty;
        public bool TranslateAllEntities { get; set; } = true;
        public List<string>? EntityIds { get; set; }
    }

    public class CurrencyTranslationResult
    {
        public bool Success { get; set; }
        public string GroupId { get; set; } = string.Empty;
        public int EntitiesTranslated { get; set; }
        public string ReportingCurrency { get; set; } = string.Empty;
        public decimal TotalTranslationDifference { get; set; }
        public List<EntityTranslationDetail> Details { get; set; } = new();
        public DateTime CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EntityTranslationDetail
    {
        public string EntityId { get; set; } = string.Empty;
        public string EntityCode { get; set; } = string.Empty;
        public string FunctionalCurrency { get; set; } = string.Empty;
        public decimal TranslationDifference { get; set; }
        public decimal BalanceSheetRate { get; set; }
        public decimal IncomeStatementRate { get; set; }
        public decimal EquityRate { get; set; }
    }

    public class EliminationsRunRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public List<string>? EntityIds { get; set; }
        public bool RunAllEliminations { get; set; } = true;
        public string? EliminationType { get; set; }
    }

    public class EliminationsRunResult
    {
        public bool Success { get; set; }
        public string GroupId { get; set; } = string.Empty;
        public int EliminationsProcessed { get; set; }
        public decimal TotalEliminationAmount { get; set; }
        public List<EliminationDetail> Eliminations { get; set; } = new();
        public DateTime CompletedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EliminationDetail
    {
        public string EliminationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FromEntityId { get; set; } = string.Empty;
        public string ToEntityId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? JournalEntryReference { get; set; }
    }

    public class MinorityInterestRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string CalculationMethod { get; set; } = string.Empty;
    }

    public class MinorityInterestResult
    {
        public bool Success { get; set; }
        public string GroupId { get; set; } = string.Empty;
        public decimal TotalMinorityInterest { get; set; }
        public List<MinorityInterestEntity> Entities { get; set; } = new();
        public DateTime CalculatedAt { get; set; }
    }

    public class MinorityInterestEntity
    {
        public string EntityId { get; set; } = string.Empty;
        public string EntityCode { get; set; } = string.Empty;
        public decimal OwnershipPercentage { get; set; }
        public decimal MinorityPercentage { get; set; }
        public decimal NetIncome { get; set; }
        public decimal MinorityInterestAmount { get; set; }
        public decimal Equity { get; set; }
        public decimal MinorityEquityPortion { get; set; }
    }

    public class ConsolidationReportRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string ReportType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public bool IncludeDetails { get; set; } = true;
    }

    public class ConsolidationReportResult
    {
        public bool Success { get; set; }
        public string ReportUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int PageCount { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class InterCompanyReconciliationRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public bool AutoEliminate { get; set; } = false;
    }

    public class InterCompanyReconciliationResult
    {
        public bool Success { get; set; }
        public string GroupId { get; set; } = string.Empty;
        public decimal TotalInterCompanyTransactions { get; set; }
        public decimal ReconciledAmount { get; set; }
        public decimal UnreconciledAmount { get; set; }
        public bool IsFullyReconciled { get; set; }
        public List<InterCompanyReconciliationLine> Lines { get; set; } = new();
        public DateTime ReconciledAt { get; set; }
    }

    public class InterCompanyReconciliationLine
    {
        public string FromEntityCode { get; set; } = string.Empty;
        public string ToEntityCode { get; set; } = string.Empty;
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal FromEntityAmount { get; set; }
        public decimal ToEntityAmount { get; set; }
        public decimal Difference { get; set; }
        public string Currency { get; set; } = string.Empty;
        public bool IsReconciled { get; set; }
    }

    public class ConsolidatedBalanceSheetRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public bool ComparativeMode { get; set; } = false;
        public string? ComparativeFiscalYear { get; set; }
    }

    public class ConsolidatedBalanceSheetResult
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public List<ConsolidatedBSLineItem> Assets { get; set; } = new();
        public List<ConsolidatedBSLineItem> Liabilities { get; set; } = new();
        public List<ConsolidatedBSLineItem> Equity { get; set; } = new();
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public decimal MinorityInterest { get; set; }
        public bool IsBalanced { get; set; }
    }

    public class ConsolidatedBSLineItem
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal? ComparativeAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int HierarchyLevel { get; set; }
        public List<string> ContributingEntities { get; set; } = new();
    }

    public class ConsolidatedPnLRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int PeriodFrom { get; set; }
        public int PeriodTo { get; set; }
        public bool ComparativeMode { get; set; } = false;
        public string? ComparativeFiscalYear { get; set; }
    }

    public class ConsolidatedPnLResult
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public List<ConsolidatedPnLCategory> Categories { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal ConsolidatedNetIncome { get; set; }
        public decimal MinorityInterestShare { get; set; }
        public decimal NetIncomeAttributableToParent { get; set; }
    }

    public class ConsolidatedPnLCategory
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal? ComparativeAmount { get; set; }
        public List<ConsolidatedPnLLineItem> LineItems { get; set; } = new();
    }

    public class ConsolidatedPnLLineItem
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public List<string> ContributingEntities { get; set; } = new();
    }

    public class TransactionEliminateRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string FromEntityId { get; set; } = string.Empty;
        public string ToEntityId { get; set; } = string.Empty;
        public string AccountCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TransactionEliminateResult
    {
        public bool Success { get; set; }
        public string EliminationId { get; set; } = string.Empty;
        public string JournalEntryReference { get; set; } = string.Empty;
        public decimal EliminatedAmount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ConsolidationExportRequest
    {
        public string GroupId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string ExportFormat { get; set; } = string.Empty;
        public bool IncludeTrialBalance { get; set; } = true;
        public bool IncludeEliminations { get; set; } = true;
        public bool IncludeTranslation { get; set; } = true;
        public bool IncludeInterCompany { get; set; } = true;
    }

    public class ConsolidationExportResult
    {
        public bool Success { get; set; }
        public string ExportUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime ExportedAt { get; set; }
    }

    public interface IConsolidationService
    {
        Task<ConsolidationGroupCreateResult> CreateConsolidationGroupAsync(ConsolidationGroupCreateRequest request);
        Task<ConsolidationEntityAddResult> AddEntityAsync(ConsolidationEntityAddRequest request);
        Task<ConsolidationEntityDataSubmitResult> SubmitEntityDataAsync(ConsolidationEntityDataSubmitRequest request);
        Task<ConsolidationEntityDataValidateResult> ValidateEntityDataAsync(ConsolidationEntityDataValidateRequest request);
        Task<CurrencyTranslationResult> RunCurrencyTranslationAsync(CurrencyTranslationRequest request);
        Task<EliminationsRunResult> RunEliminationsAsync(EliminationsRunRequest request);
        Task<MinorityInterestResult> CalculateMinorityInterestAsync(MinorityInterestRequest request);
        Task<ConsolidationReportResult> GenerateConsolidationReportAsync(ConsolidationReportRequest request);
        Task<InterCompanyReconciliationResult> GetInterCompanyReconciliationAsync(InterCompanyReconciliationRequest request);
        Task<ConsolidatedBalanceSheetResult> GetConsolidatedBalanceSheetAsync(ConsolidatedBalanceSheetRequest request);
        Task<ConsolidatedPnLResult> GetConsolidatedPnLAsync(ConsolidatedPnLRequest request);
        Task<TransactionEliminateResult> EliminateTransactionAsync(TransactionEliminateRequest request);
        Task<ConsolidationExportResult> ExportConsolidationAsync(ConsolidationExportRequest request);
    }
}
