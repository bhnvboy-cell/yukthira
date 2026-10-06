using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YuktiraERP.Core.Interfaces
{
    public class DutyAssignmentRequest
    {
        public Guid UserId { get; set; }
        public string DutyType { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid AssignedByUserId { get; set; }
    }

    public class DutyAssignmentResult
    {
        public bool Success { get; set; }
        public Guid AssignmentId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DutyRevokeRequest
    {
        public Guid AssignmentId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid RevokedByUserId { get; set; }
    }

    public class DutyRevokeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DutyConflictCheckRequest
    {
        public Guid UserId { get; set; }
        public string DutyType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class DutyConflictCheckResult
    {
        public bool HasConflict { get; set; }
        public List<DutyConflictDetail> Conflicts { get; set; } = new();
    }

    public class DutyConflictDetail
    {
        public Guid AssignmentId { get; set; }
        public string DutyType { get; set; } = string.Empty;
        public DateTime OverlapStart { get; set; }
        public DateTime OverlapEnd { get; set; }
    }

    public class SeparationValidationRequest
    {
        public Guid UserId { get; set; }
        public DateTime EffectiveDate { get; set; }
    }

    public class SeparationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Violations { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class ViolationQueryRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetViolationsResult
    {
        public List<SoxDetectedViolation> Violations { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class SoxDetectedViolation
    {
        public Guid ViolationId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime DetectedDate { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ResolveViolationRequest
    {
        public Guid ViolationId { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public string RootCause { get; set; } = string.Empty;
        public Guid ResolvedByUserId { get; set; }
    }

    public class ResolveViolationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AuditTrailLogRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public Dictionary<string, string>? OldValues { get; set; }
        public Dictionary<string, string>? NewValues { get; set; }
    }

    public class AuditTrailLogResult
    {
        public bool Success { get; set; }
        public Guid AuditEntryId { get; set; }
        public DateTime LoggedAt { get; set; }
    }

    public class AuditIntegrityVerifyRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
    }

    public class AuditIntegrityVerifyResult
    {
        public bool IsIntact { get; set; }
        public int TotalEntriesChecked { get; set; }
        public int TamperedEntries { get; set; }
        public List<AuditIntegrityIssue> Issues { get; set; } = new();
    }

    public class AuditIntegrityIssue
    {
        public Guid AuditEntryId { get; set; }
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AuditChainVerifyRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class AuditChainVerifyResult
    {
        public bool ChainValid { get; set; }
        public int ChainLength { get; set; }
        public int BrokenLinks { get; set; }
        public List<AuditChainLink> BrokenLinksDetails { get; set; } = new();
    }

    public class AuditChainLink
    {
        public Guid PreviousEntryId { get; set; }
        public Guid CurrentEntryId { get; set; }
        public string MismatchType { get; set; } = string.Empty;
    }

    public class AuditTrailQueryRequest
    {
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public Guid? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Action { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetAuditTrailResult
    {
        public List<AuditTrailEntry> Entries { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class AuditTrailEntry
    {
        public Guid EntryId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; } = string.Empty;
    }

    public class AuditReportExportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? EntityType { get; set; }
        public string Format { get; set; } = "PDF";
        public bool IncludeDetails { get; set; } = true;
    }

    public class AuditReportExportResult
    {
        public bool Success { get; set; }
        public string ReportUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }

    public class SoxDutyDto
    {
        public Guid Id { get; set; }
        public string DutyCode { get; set; } = string.Empty;
        public string DutyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string TransactionCode { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public int MinApprovers { get; set; } = 1;
        public List<string> RequiredRoles { get; set; } = new();
        public List<string> ConflictDuties { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class SoxAssignmentDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string DutyCode { get; set; } = string.Empty;
        public string DutyName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public string AssignedBy { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
        public string Source { get; set; } = "Explicit";
    }

    public class SoxDutySaveRequest
    {
        public string DutyCode { get; set; } = string.Empty;
        public string DutyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string TransactionCode { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public int MinApprovers { get; set; } = 1;
        public List<string> RequiredRoles { get; set; } = new();
        public List<string> ConflictDuties { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class SoxAssignRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string DutyCode { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }

    public class SoDScanResult
    {
        public int UsersScanned { get; set; }
        public int DutiesEvaluated { get; set; }
        public int NewViolations { get; set; }
        public int ExistingSkipped { get; set; }
        public List<SoxDetectedViolation> Violations { get; set; } = new();
        public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    }

    public class SoDDutyConflictPair
    {
        public string DutyA { get; set; } = string.Empty;
        public string DutyB { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
    }

    public interface ISoxComplianceService
    {
        Task<DutyAssignmentResult> AssignDutyAsync(DutyAssignmentRequest request);
        Task<DutyRevokeResult> RevokeDutyAsync(DutyRevokeRequest request);
        Task<DutyConflictCheckResult> CheckDutyConflictAsync(DutyConflictCheckRequest request);
        Task<SeparationValidationResult> ValidateSeparationAsync(SeparationValidationRequest request);
        Task<GetViolationsResult> GetViolationsAsync(ViolationQueryRequest request);
        Task<ResolveViolationResult> ResolveViolationAsync(ResolveViolationRequest request);
        Task<AuditTrailLogResult> LogAuditTrailAsync(AuditTrailLogRequest request);
        Task<AuditIntegrityVerifyResult> VerifyAuditIntegrityAsync(AuditIntegrityVerifyRequest request);
        Task<AuditChainVerifyResult> VerifyAuditChainAsync(AuditChainVerifyRequest request);
        Task<GetAuditTrailResult> GetAuditTrailAsync(AuditTrailQueryRequest request);
        Task<AuditReportExportResult> ExportAuditReportAsync(AuditReportExportRequest request);

        Task<List<SoxDutyDto>> GetDutiesAsync(Guid tenantId, bool includeInactive = false);
        Task<SoxDutyDto?> CreateDutyAsync(Guid tenantId, SoxDutySaveRequest request, string actor);
        Task<SoxDutyDto?> UpdateDutyAsync(Guid tenantId, Guid dutyId, SoxDutySaveRequest request, string actor);
        Task<bool> DeleteDutyAsync(Guid tenantId, Guid dutyId);
        Task<List<SoxAssignmentDto>> GetAssignmentsAsync(Guid tenantId, string? userId = null, bool activeOnly = true);
        Task<DutyAssignmentResult> AssignDutyToUserAsync(Guid tenantId, SoxAssignRequest request);
        Task<DutyRevokeResult> DeactivateAssignmentAsync(Guid tenantId, Guid assignmentId, string actor);
        Task<SoDScanResult> RunSoDScanAsync(Guid tenantId, string scannedBy);
        Task<List<SoxAssignmentDto>> GetEffectiveDutiesAsync(Guid tenantId, string userId);
        Task ExportViolationsToCsvAsync(Guid tenantId, Stream stream, string? status = null, string? severity = null);
    }
}
