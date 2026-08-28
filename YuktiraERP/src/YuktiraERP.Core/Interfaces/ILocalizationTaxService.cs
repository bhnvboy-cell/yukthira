using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class LocalizationTaxCalculationRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public string TaxType { get; set; } = string.Empty;
        public decimal BaseAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? City { get; set; }
        public string? TaxCode { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? CustomerTaxId { get; set; }
        public bool IsExempt { get; set; } = false;
        public string? ExemptionCertificate { get; set; }
        public List<LocalizationTaxLineItem>? LineItems { get; set; }
    }

    public class LocalizationTaxLineItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string HsnCode { get; set; } = string.Empty;
        public bool IsTaxable { get; set; } = true;
    }

    public class TaxCalculationResult
    {
        public bool Success { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public decimal BaseAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalAmountWithTax { get; set; }
        public List<TaxDetail> Taxes { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class TaxDetail
    {
        public string TaxType { get; set; } = string.Empty;
        public string TaxName { get; set; } = string.Empty;
        public string TaxRate { get; set; } = string.Empty;
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string TaxCode { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? Description { get; set; }
    }

    public class SupportedCountriesRequest
    {
        public string? TaxType { get; set; }
        public bool ActiveOnly { get; set; } = true;
    }

    public class SupportedCountriesResult
    {
        public List<CountryTaxInfo> Countries { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class CountryTaxInfo
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public bool IsSupported { get; set; }
        public List<string> SupportedTaxTypes { get; set; } = new();
        public List<string> States { get; set; } = new();
        public string? TaxAuthorityWebsite { get; set; }
    }

    public class TaxConfigRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public string? State { get; set; }
    }

    public class TaxConfigResult
    {
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public List<TaxConfigItem> Configurations { get; set; } = new();
    }

    public class TaxConfigItem
    {
        public string TaxType { get; set; } = string.Empty;
        public string TaxName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public string RateType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? MinAmount { get; set; }
        public string? MaxAmount { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? Description { get; set; }
    }

    public class TaxConfigCreateRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public string? State { get; set; }
        public string TaxType { get; set; } = string.Empty;
        public string TaxName { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public string RateType { get; set; } = string.Empty;
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? Description { get; set; }
    }

    public class TaxConfigCreateResult
    {
        public bool Success { get; set; }
        public string ConfigId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class TaxReturnGenerateRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public string TaxType { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string? State { get; set; }
        public bool IncludePreviousPeriods { get; set; } = false;
    }

    public class TaxReturnGenerateResult
    {
        public bool Success { get; set; }
        public string ReturnId { get; set; } = string.Empty;
        public string ReturnPeriod { get; set; } = string.Empty;
        public decimal TotalTaxableAmount { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal NetTaxDue { get; set; }
        public List<TaxReturnLineItem> LineItems { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class TaxReturnLineItem
    {
        public string LineNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TaxCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class TaxReturnValidateRequest
    {
        public string ReturnId { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string TaxType { get; set; } = string.Empty;
    }

    public class TaxReturnValidateResult
    {
        public bool IsValid { get; set; }
        public List<TaxReturnValidationMessage> Messages { get; set; } = new();
        public bool IsReadyToSubmit { get; set; }
    }

    public class TaxReturnValidationMessage
    {
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public class TaxReturnFileRequest
    {
        public string ReturnId { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string TaxType { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string FilingType { get; set; } = string.Empty;
        public bool ElectronicFiling { get; set; } = true;
    }

    public class TaxReturnFileResult
    {
        public bool Success { get; set; }
        public string FilingReference { get; set; } = string.Empty;
        public DateTime FiledAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AcknowledgementNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class WhtCalculationRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string? ContractorTaxId { get; set; }
        public bool IsResident { get; set; } = true;
        public string? SectionCode { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class WhtCalculationResult
    {
        public bool Success { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal WhtRate { get; set; }
        public decimal WhtAmount { get; set; }
        public decimal NetPaymentAmount { get; set; }
        public string SectionCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class WhtEntryPostRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal WhtAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string ContractorName { get; set; } = string.Empty;
        public string ContractorTaxId { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string SectionCode { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string? InvoiceReference { get; set; }
    }

    public class WhtEntryPostResult
    {
        public bool Success { get; set; }
        public string EntryId { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime PostedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class WhtSummaryRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public string? ContractorTaxId { get; set; }
        public string? PaymentType { get; set; }
    }

    public class WhtSummaryResult
    {
        public string CountryCode { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;
        public int FiscalPeriod { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public decimal TotalWhtAmount { get; set; }
        public int TotalEntries { get; set; }
        public List<WhtSummaryByType> ByPaymentType { get; set; } = new();
        public List<WhtSummaryBySection> BySection { get; set; } = new();
        public List<WhtSummaryByContractor> ByContractor { get; set; } = new();
    }

    public class WhtSummaryByType
    {
        public string PaymentType { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal WhtAmount { get; set; }
        public int EntryCount { get; set; }
    }

    public class WhtSummaryBySection
    {
        public string SectionCode { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal WhtAmount { get; set; }
        public int EntryCount { get; set; }
    }

    public class WhtSummaryByContractor
    {
        public string ContractorName { get; set; } = string.Empty;
        public string ContractorTaxId { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal WhtAmount { get; set; }
        public int EntryCount { get; set; }
    }

    public class TaxAuditReportRequest
    {
        public string CountryCode { get; set; } = string.Empty;
        public string TaxType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? TaxId { get; set; }
        public string Format { get; set; } = string.Empty;
    }

    public class TaxAuditReportResult
    {
        public bool Success { get; set; }
        public string ReportUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class GstValidationRequest
    {
        public string GstNumber { get; set; } = string.Empty;
    }

    public class GstValidationResult
    {
        public bool IsValid { get; set; }
        public string GstNumber { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public string TradeName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string RegistrationDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string Constitution { get; set; } = string.Empty;
    }

    public class HsnCodeRequest
    {
        public string Description { get; set; } = string.Empty;
        public string? ProductCategory { get; set; }
        public string? MaterialNumber { get; set; }
        public int MaxResults { get; set; } = 10;
    }

    public class HsnCodeResult
    {
        public List<HsnCodeItem> Codes { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class HsnCodeItem
    {
        public string HsnCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal GstRate { get; set; }
        public string? CessRate { get; set; }
        public bool IsApplicable { get; set; }
    }

    public interface ILocalizationTaxService
    {
        Task<TaxCalculationResult> CalculateTaxAsync(LocalizationTaxCalculationRequest request);
        Task<SupportedCountriesResult> GetSupportedCountriesAsync(SupportedCountriesRequest request);
        Task<TaxConfigResult> GetTaxConfigAsync(TaxConfigRequest request);
        Task<TaxConfigCreateResult> CreateTaxConfigAsync(TaxConfigCreateRequest request);
        Task<TaxReturnGenerateResult> GenerateTaxReturnAsync(TaxReturnGenerateRequest request);
        Task<TaxReturnValidateResult> ValidateTaxReturnAsync(TaxReturnValidateRequest request);
        Task<TaxReturnFileResult> FileTaxReturnAsync(TaxReturnFileRequest request);
        Task<WhtCalculationResult> CalculateWHTAsync(WhtCalculationRequest request);
        Task<WhtEntryPostResult> PostWHTEntryAsync(WhtEntryPostRequest request);
        Task<WhtSummaryResult> GetWHTSummaryAsync(WhtSummaryRequest request);
        Task<TaxAuditReportResult> GetTaxAuditReportAsync(TaxAuditReportRequest request);
        Task<GstValidationResult> ValidateGSTINAsync(GstValidationRequest request);
        Task<HsnCodeResult> GetHSNCodeAsync(HsnCodeRequest request);
    }
}
