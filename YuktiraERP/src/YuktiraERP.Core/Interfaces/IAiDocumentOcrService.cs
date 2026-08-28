using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class DocumentUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public string UploadedByUserId { get; set; } = string.Empty;
    }

    public class DocumentUploadResult
    {
        public bool Success { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string StorageUrl { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DocumentProcessRequest
    {
        public string DocumentId { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public bool RunOcr { get; set; } = true;
        public bool RunClassification { get; set; } = true;
        public bool RunExtraction { get; set; } = true;
        public string? TemplateId { get; set; }
        public string LanguageCode { get; set; } = "en";
        public bool EnhanceImage { get; set; } = true;
        public bool DeskewImage { get; set; } = true;
    }

    public class DocumentProcessResult
    {
        public bool Success { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DocumentClassification { get; set; } = string.Empty;
        public decimal ConfidenceScore { get; set; }
        public int PagesProcessed { get; set; }
        public int FieldsExtracted { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ExtractedDataRequest
    {
        public string DocumentId { get; set; } = string.Empty;
        public bool IncludeRawText { get; set; } = false;
        public bool IncludeConfidenceScores { get; set; } = true;
    }

    public class ExtractedDataResult
    {
        public string DocumentId { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public List<ExtractedField> Fields { get; set; } = new();
        public List<ExtractedTable> Tables { get; set; } = new();
        public string? RawText { get; set; }
        public decimal OverallConfidence { get; set; }
        public DateTime ExtractedAt { get; set; }
    }

    public class ExtractedField
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldValue { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public decimal ConfidenceScore { get; set; }
        public int? PageNumber { get; set; }
        public BoundingBox? Location { get; set; }
        public bool IsValid { get; set; } = true;
    }

    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class ExtractedTable
    {
        public string TableName { get; set; } = string.Empty;
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
        public int? PageNumber { get; set; }
        public decimal ConfidenceScore { get; set; }
    }

    public class ExtractedDataValidateRequest
    {
        public string DocumentId { get; set; } = string.Empty;
        public List<ExtractedFieldValidation>? FieldValidations { get; set; }
        public bool ValidateAgainstMasterData { get; set; } = false;
        public bool CrossValidateFields { get; set; } = true;
    }

    public class ExtractedDataValidateResult
    {
        public bool IsValid { get; set; }
        public List<FieldValidationResult> FieldValidations { get; set; } = new();
        public List<string> CrossValidationIssues { get; set; } = new();
        public decimal OverallValidityScore { get; set; }
    }

    public class ExtractedFieldValidation
    {
        public string FieldName { get; set; } = string.Empty;
        public string ExpectedFormat { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? MinValue { get; set; }
        public string? MaxValue { get; set; }
    }

    public class FieldValidationResult
    {
        public string FieldName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public class OcrTemplateCreateRequest
    {
        public string TemplateName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<TemplateFieldDefinition> Fields { get; set; } = new();
        public List<TemplateTableDefinition>? Tables { get; set; }
        public string? SampleDocumentId { get; set; }
    }

    public class OcrTemplateCreateResult
    {
        public bool Success { get; set; }
        public string TemplateId { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TemplateFieldDefinition
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public string? DefaultValue { get; set; }
        public string? ValidationPattern { get; set; }
        public BoundingBox? ApproximateLocation { get; set; }
        public string? Description { get; set; }
    }

    public class TemplateTableDefinition
    {
        public string TableName { get; set; } = string.Empty;
        public int ExpectedColumns { get; set; }
        public BoundingBox? ApproximateLocation { get; set; }
        public List<string>? ColumnHeaders { get; set; }
    }

    public class OcrTemplatesGetRequest
    {
        public string? DocumentType { get; set; }
        public string? TemplateName { get; set; }
        public bool ActiveOnly { get; set; } = true;
    }

    public class OcrTemplatesGetResult
    {
        public List<OcrTemplate> Templates { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class OcrTemplate
    {
        public string TemplateId { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int FieldCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
    }

    public class ErpMappingRequest
    {
        public string DocumentId { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string TargetErpModule { get; set; } = string.Empty;
        public string TargetErpDocumentType { get; set; } = string.Empty;
        public Dictionary<string, string>? FieldMappings { get; set; }
        public bool AutoPopulateFromExtractedData { get; set; } = true;
    }

    public class ErpMappingResult
    {
        public bool Success { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public string TargetErpModule { get; set; } = string.Empty;
        public string? ErpDocumentNumber { get; set; }
        public List<ErpMappingField> MappedFields { get; set; } = new();
        public List<ErpMappingField> UnmappedFields { get; set; } = new();
        public bool RequiresManualReview { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ErpMappingField
    {
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsMapped { get; set; }
        public string? MappingConfidence { get; set; }
    }

    public class BatchProcessRequest
    {
        public string DocumentType { get; set; } = string.Empty;
        public List<string> DocumentIds { get; set; } = new();
        public string? TemplateId { get; set; }
        public string LanguageCode { get; set; } = "en";
        public bool AutoMapToErp { get; set; } = false;
        public string? TargetErpModule { get; set; }
    }

    public class BatchProcessResult
    {
        public bool Success { get; set; }
        public string BatchId { get; set; } = string.Empty;
        public int TotalDocuments { get; set; }
        public int ProcessedDocuments { get; set; }
        public int SuccessfulDocuments { get; set; }
        public int FailedDocuments { get; set; }
        public List<BatchProcessDocumentResult> Results { get; set; } = new();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class BatchProcessDocumentResult
    {
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErpDocumentNumber { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal? ConfidenceScore { get; set; }
    }

    public class DocumentHistoryRequest
    {
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? DocumentType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class DocumentHistoryResult
    {
        public List<DocumentHistoryItem> Documents { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class DocumentHistoryItem
    {
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public decimal? ConfidenceScore { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedByTemplate { get; set; }
        public string? ErpDocumentNumber { get; set; }
        public string UploadedByUserId { get; set; } = string.Empty;
    }

    public interface IAiDocumentOcrService
    {
        Task<DocumentUploadResult> UploadDocumentAsync(DocumentUploadRequest request);
        Task<DocumentProcessResult> ProcessDocumentAsync(DocumentProcessRequest request);
        Task<ExtractedDataResult> GetExtractedDataAsync(ExtractedDataRequest request);
        Task<ExtractedDataValidateResult> ValidateExtractedDataAsync(ExtractedDataValidateRequest request);
        Task<OcrTemplateCreateResult> CreateTemplateAsync(OcrTemplateCreateRequest request);
        Task<OcrTemplatesGetResult> GetTemplatesAsync(OcrTemplatesGetRequest request);
        Task<ErpMappingResult> MapToErpAsync(ErpMappingRequest request);
        Task<BatchProcessResult> BatchProcessAsync(BatchProcessRequest request);
        Task<DocumentHistoryResult> GetDocumentHistoryAsync(DocumentHistoryRequest request);
    }
}
