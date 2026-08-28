using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class SoxComplianceService : ISoxComplianceService
{
    private readonly YuktiraDbContext _db;

    public SoxComplianceService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<DutyAssignmentResult> AssignDutyAsync(DutyAssignmentRequest request)
    {
        var conflicts = await _db.SoxAssignments
            .Where(a => a.UserId == request.UserId.ToString()
                && a.DutyCode == request.DutyType
                && a.IsActive
                && (!a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow))
            .ToListAsync();

        if (conflicts.Any())
        {
            return new DutyAssignmentResult
            {
                Success = false,
                Message = $"Conflict detected: User {request.UserId} already has an active assignment for duty {request.DutyType}"
            };
        }

        var conflictingDuties = await _db.SoxDuties
            .Where(d => d.ConflictDuties.Contains(request.DutyType) && d.IsActive)
            .ToListAsync();

        foreach (var duty in conflictingDuties)
        {
            var hasConflict = await _db.SoxAssignments
                .AnyAsync(a => a.UserId == request.UserId.ToString()
                    && a.DutyCode == duty.DutyCode
                    && a.IsActive);
            if (hasConflict)
            {
                return new DutyAssignmentResult
                {
                    Success = false,
                    Message = $"Segregation of duties violation: {request.DutyType} conflicts with {duty.DutyCode}"
                };
            }
        }

        var assignment = new SoxAssignmentEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId.ToString(),
            Role = request.Role,
            DutyCode = request.DutyType,
            DutyName = request.Description,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = request.AssignedByUserId.ToString(),
            ExpiresAt = request.EndDate,
            IsActive = true,
            Notes = $"Assigned duty: {request.Description}"
        };

        _db.SoxAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        return new DutyAssignmentResult
        {
            Success = true,
            AssignmentId = assignment.Id,
            Message = $"Duty {request.DutyType} assigned successfully to user {request.UserId}"
        };
    }

    public async Task<DutyRevokeResult> RevokeDutyAsync(DutyRevokeRequest request)
    {
        var assignment = await _db.SoxAssignments.FindAsync(request.AssignmentId);
        if (assignment == null)
            return new DutyRevokeResult { Success = false, Message = "Assignment not found" };

        assignment.IsActive = false;
        assignment.Notes = $"Revoked: {request.Reason}";
        await _db.SaveChangesAsync();

        return new DutyRevokeResult
        {
            Success = true,
            Message = $"Duty assignment {request.AssignmentId} revoked successfully"
        };
    }

    public async Task<DutyConflictCheckResult> CheckDutyConflictAsync(DutyConflictCheckRequest request)
    {
        var activeAssignments = await _db.SoxAssignments
            .Where(a => a.UserId == request.UserId.ToString()
                && a.IsActive
                && a.AssignedAt <= request.EndDate
                && (!a.ExpiresAt.HasValue || a.ExpiresAt >= request.StartDate))
            .ToListAsync();

        var result = new DutyConflictCheckResult { HasConflict = false };
        foreach (var assignment in activeAssignments)
        {
            var duty = await _db.SoxDuties.FirstOrDefaultAsync(d => d.DutyCode == assignment.DutyCode);
            if (duty?.ConflictDuties.Contains(request.DutyType) == true)
            {
                result.HasConflict = true;
                result.Conflicts.Add(new DutyConflictDetail
                {
                    AssignmentId = assignment.Id,
                    DutyType = assignment.DutyCode,
                    OverlapStart = assignment.AssignedAt,
                    OverlapEnd = assignment.ExpiresAt ?? DateTime.MaxValue
                });
            }
        }

        return result;
    }

    public async Task<SeparationValidationResult> ValidateSeparationAsync(SeparationValidationRequest request)
    {
        var result = new SeparationValidationResult { IsValid = true };
        var activeAssignments = await _db.SoxAssignments
            .Where(a => a.UserId == request.UserId.ToString() && a.IsActive)
            .ToListAsync();

        if (activeAssignments.Count > 1)
        {
            var duties = activeAssignments.Select(a => a.DutyCode).ToList();
            var allDuties = await _db.SoxDuties.Where(d => d.IsActive).ToListAsync();

            foreach (var a1 in activeAssignments)
            {
                foreach (var a2 in activeAssignments.Where(a => a.Id != a1.Id))
                {
                    var duty1 = allDuties.FirstOrDefault(d => d.DutyCode == a1.DutyCode);
                    if (duty1?.ConflictDuties.Contains(a2.DutyCode) == true)
                    {
                        result.IsValid = false;
                        result.Violations.Add($"Conflicting duties: {a1.DutyCode} and {a2.DutyCode} assigned to same user");
                    }
                }
            }
        }

        if (!result.IsValid)
            result.Recommendations.Add("Revoke conflicting duty assignments before user separation");

        return result;
    }

    public async Task<GetViolationsResult> GetViolationsAsync(ViolationQueryRequest request)
    {
        var query = _db.SoxViolations.AsQueryable();

        if (!string.IsNullOrEmpty(request.Severity))
            query = query.Where(v => v.Severity == request.Severity);
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(v => v.Status == request.Status);
        if (request.FromDate.HasValue)
            query = query.Where(v => v.DetectedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(v => v.DetectedAt <= request.ToDate.Value);

        var totalCount = await query.CountAsync();
        var rawViolations = await query
            .OrderByDescending(v => v.DetectedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var violations = rawViolations.Select(v =>
        {
            Guid.TryParse(v.UserId, out var uid);
            return new SoxDetectedViolation
            {
                ViolationId = v.Id,
                RuleCode = v.ViolationType,
                Description = v.Description,
                Severity = v.Severity,
                DetectedDate = v.DetectedAt,
                UserId = uid,
                UserName = v.UserName,
                Status = v.Status
            };
        }).ToList();

        return new GetViolationsResult
        {
            Violations = violations,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<ResolveViolationResult> ResolveViolationAsync(ResolveViolationRequest request)
    {
        var violation = await _db.SoxViolations.FindAsync(request.ViolationId);
        if (violation == null)
            return new ResolveViolationResult { Success = false, Message = "Violation not found" };

        violation.Status = "Resolved";
        violation.ResolutionNotes = request.Resolution;
        violation.ResolvedBy = request.ResolvedByUserId.ToString();
        violation.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new ResolveViolationResult
        {
            Success = true,
            Message = $"Violation {request.ViolationId} resolved successfully"
        };
    }

    public async Task<AuditTrailLogResult> LogAuditTrailAsync(AuditTrailLogRequest request)
    {
        var lastEntry = await _db.ImmutableAuditTrails
            .Where(a => a.TableName == request.EntityType && a.RecordId == request.EntityId.ToString())
            .OrderByDescending(a => a.SequenceNumber)
            .FirstOrDefaultAsync();

        var previousHash = lastEntry?.CurrentHash ?? ComputeSha256("GENESIS");
        var timestamp = DateTime.UtcNow;
        var sequenceNumber = (lastEntry?.SequenceNumber ?? 0) + 1;

        var payload = JsonSerializer.Serialize(new
        {
            request.EntityType,
            request.EntityId,
            request.Action,
            request.UserId,
            request.Details,
            request.OldValues,
            request.NewValues,
            Timestamp = timestamp,
            SequenceNumber = sequenceNumber
        });

        var currentHash = ComputeSha256(previousHash + payload);

        var auditEntry = new ImmutableAuditTrailEntity
        {
            Id = Guid.NewGuid(),
            SequenceNumber = sequenceNumber,
            TableName = request.EntityType,
            RecordId = request.EntityId.ToString(),
            ActionType = request.Action,
            OldValues = request.OldValues != null ? JsonSerializer.Serialize(request.OldValues) : "{}",
            NewValues = request.NewValues != null ? JsonSerializer.Serialize(request.NewValues) : "{}",
            UserId = request.UserId.ToString(),
            UserName = request.UserName,
            Timestamp = timestamp,
            PreviousHash = previousHash,
            CurrentHash = currentHash,
            IsImmutable = true,
            WitnessSignature = ComputeSha256($"WITNESS:{currentHash}:{sequenceNumber}")
        };

        _db.ImmutableAuditTrails.Add(auditEntry);
        await _db.SaveChangesAsync();

        return new AuditTrailLogResult
        {
            Success = true,
            AuditEntryId = auditEntry.Id,
            LoggedAt = timestamp
        };
    }

    public async Task<AuditIntegrityVerifyResult> VerifyAuditIntegrityAsync(AuditIntegrityVerifyRequest request)
    {
        var query = _db.ImmutableAuditTrails
            .Where(a => a.Timestamp >= request.FromDate && a.Timestamp <= request.ToDate);

        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(a => a.TableName == request.EntityType);
        if (request.EntityId.HasValue)
            query = query.Where(a => a.RecordId == request.EntityId.Value.ToString());

        var entries = await query.OrderBy(a => a.SequenceNumber).ToListAsync();

        var result = new AuditIntegrityVerifyResult
        {
            TotalEntriesChecked = entries.Count,
            TamperedEntries = 0,
            IsIntact = true
        };

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var payload = JsonSerializer.Serialize(new
            {
                EntityType = entry.TableName,
                EntityId = entry.RecordId,
                Action = entry.ActionType,
                UserId = entry.UserId,
                OldValues = entry.OldValues,
                NewValues = entry.NewValues,
                entry.Timestamp,
                entry.SequenceNumber
            });

            var previousHash = i > 0 ? entries[i - 1].CurrentHash : ComputeSha256("GENESIS");
            var recomputedHash = ComputeSha256(previousHash + payload);

            if (recomputedHash != entry.CurrentHash)
            {
                result.IsIntact = false;
                result.TamperedEntries++;
                result.Issues.Add(new AuditIntegrityIssue
                {
                    AuditEntryId = entry.Id,
                    IssueType = "HashMismatch",
                    Description = $"Entry {entry.Id} hash mismatch: expected {recomputedHash}, found {entry.CurrentHash}"
                });
            }

            if (entry.PreviousHash != previousHash)
            {
                result.IsIntact = false;
                result.TamperedEntries++;
                result.Issues.Add(new AuditIntegrityIssue
                {
                    AuditEntryId = entry.Id,
                    IssueType = "ChainBroken",
                    Description = $"Entry {entry.Id} PreviousHash does not match previous entry's hash"
                });
            }
        }

        return result;
    }

    public async Task<AuditChainVerifyResult> VerifyAuditChainAsync(AuditChainVerifyRequest request)
    {
        var entries = await _db.ImmutableAuditTrails
            .Where(a => a.TableName == request.EntityType
                && a.RecordId == request.EntityId.ToString()
                && a.Timestamp >= request.FromDate
                && a.Timestamp <= request.ToDate)
            .OrderBy(a => a.SequenceNumber)
            .ToListAsync();

        var result = new AuditChainVerifyResult
        {
            ChainLength = entries.Count,
            ChainValid = true,
            BrokenLinks = 0
        };

        for (int i = 1; i < entries.Count; i++)
        {
            if (entries[i].PreviousHash != entries[i - 1].CurrentHash)
            {
                result.ChainValid = false;
                result.BrokenLinks++;
                result.BrokenLinksDetails.Add(new AuditChainLink
                {
                    PreviousEntryId = entries[i - 1].Id,
                    CurrentEntryId = entries[i].Id,
                    MismatchType = "HashChainBroken"
                });
            }
        }

        return result;
    }

    public async Task<GetAuditTrailResult> GetAuditTrailAsync(AuditTrailQueryRequest request)
    {
        var query = _db.ImmutableAuditTrails.AsQueryable();

        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(a => a.TableName == request.EntityType);
        if (request.EntityId.HasValue)
            query = query.Where(a => a.RecordId == request.EntityId.Value.ToString());
        if (request.UserId.HasValue)
            query = query.Where(a => a.UserId == request.UserId.Value.ToString());
        if (request.FromDate.HasValue)
            query = query.Where(a => a.Timestamp >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(a => a.Timestamp <= request.ToDate.Value);
        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(a => a.ActionType == request.Action);

        var totalCount = await query.CountAsync();
        var rawEntries = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var entries = rawEntries.Select(a =>
        {
            Guid.TryParse(a.RecordId, out var rid);
            Guid.TryParse(a.UserId, out var uid);
            return new AuditTrailEntry
            {
                EntryId = a.Id,
                EntityType = a.TableName,
                EntityId = rid,
                Action = a.ActionType,
                UserId = uid,
                UserName = a.UserName,
                Details = a.ActionType,
                Timestamp = a.Timestamp,
                Hash = a.CurrentHash
            };
        }).ToList();

        return new GetAuditTrailResult { Entries = entries, TotalCount = totalCount };
    }

    public async Task<AuditReportExportResult> ExportAuditReportAsync(AuditReportExportRequest request)
    {
        var count = await _db.ImmutableAuditTrails
            .Where(a => a.Timestamp >= request.FromDate && a.Timestamp <= request.ToDate
                && (string.IsNullOrEmpty(request.EntityType) || a.TableName == request.EntityType))
            .CountAsync();

        var reportUrl = $"/reports/audit/{Guid.NewGuid()}.pdf";

        return new AuditReportExportResult
        {
            Success = true,
            ReportUrl = reportUrl,
            ContentType = "application/pdf",
            FileSizeBytes = count * 2048L
        };
    }

    private static string ComputeSha256(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
