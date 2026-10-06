using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Core.Security;
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
        var query = _db.ImmutableAuditTrails
            .Where(a => a.Timestamp >= request.FromDate && a.Timestamp <= request.ToDate);
        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(a => a.TableName == request.EntityType);
        if (request.EntityId != Guid.Empty)
            query = query.Where(a => a.RecordId == request.EntityId.ToString());

        var entries = await query
            .OrderBy(a => a.TableName)
            .ThenBy(a => a.RecordId)
            .ThenBy(a => a.SequenceNumber)
            .ToListAsync();

        var result = new AuditChainVerifyResult
        {
            ChainLength = entries.Count,
            ChainValid = true,
            BrokenLinks = 0
        };

        foreach (var chain in entries.GroupBy(a => new { a.TableName, a.RecordId }))
        {
            var previousId = Guid.Empty;
            string? previousHash = null;
            foreach (var entry in chain)
            {
                if (previousHash != null && entry.PreviousHash != previousHash)
                {
                    result.ChainValid = false;
                    result.BrokenLinks++;
                    result.BrokenLinksDetails.Add(new AuditChainLink
                    {
                        PreviousEntryId = previousId,
                        CurrentEntryId = entry.Id,
                        MismatchType = "HashChainBroken"
                    });
                }
                previousId = entry.Id;
                previousHash = entry.CurrentHash;
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

    public async Task<List<SoxDutyDto>> GetDutiesAsync(Guid tenantId, bool includeInactive = false)
    {
        var query = _db.SoxDuties.Where(d => d.TenantId == tenantId);
        if (!includeInactive) query = query.Where(d => d.IsActive);
        var duties = await query.OrderBy(d => d.DutyCode).ToListAsync();
        return duties.Select(ToDutyDto).ToList();
    }

    public async Task<SoxDutyDto?> CreateDutyAsync(Guid tenantId, SoxDutySaveRequest request, string actor)
    {
        if (string.IsNullOrWhiteSpace(request.DutyCode)) return null;
        var exists = await _db.SoxDuties.AnyAsync(d => d.TenantId == tenantId && d.DutyCode == request.DutyCode);
        if (exists) return null;

        var duty = new SoxDutyEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DutyCode = request.DutyCode,
            DutyName = request.DutyName,
            Description = request.Description,
            Module = request.Module,
            TransactionCode = request.TransactionCode,
            ActionType = request.ActionType,
            MinApprovers = request.MinApprovers,
            RequiredRoles = JsonSerializer.Serialize(request.RequiredRoles ?? new List<string>()),
            ConflictDuties = JsonSerializer.Serialize(request.ConflictDuties ?? new List<string>()),
            IsActive = request.IsActive,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo
        };
        _db.SoxDuties.Add(duty);
        await _db.SaveChangesAsync();
        return ToDutyDto(duty);
    }

    public async Task<SoxDutyDto?> UpdateDutyAsync(Guid tenantId, Guid dutyId, SoxDutySaveRequest request, string actor)
    {
        var duty = await _db.SoxDuties.FirstOrDefaultAsync(d => d.Id == dutyId && d.TenantId == tenantId);
        if (duty == null) return null;

        duty.DutyCode = request.DutyCode;
        duty.DutyName = request.DutyName;
        duty.Description = request.Description;
        duty.Module = request.Module;
        duty.TransactionCode = request.TransactionCode;
        duty.ActionType = request.ActionType;
        duty.MinApprovers = request.MinApprovers;
        duty.RequiredRoles = JsonSerializer.Serialize(request.RequiredRoles ?? new List<string>());
        duty.ConflictDuties = JsonSerializer.Serialize(request.ConflictDuties ?? new List<string>());
        duty.IsActive = request.IsActive;
        duty.EffectiveFrom = request.EffectiveFrom;
        duty.EffectiveTo = request.EffectiveTo;
        duty.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDutyDto(duty);
    }

    public async Task<bool> DeleteDutyAsync(Guid tenantId, Guid dutyId)
    {
        var duty = await _db.SoxDuties.FirstOrDefaultAsync(d => d.Id == dutyId && d.TenantId == tenantId);
        if (duty == null) return false;
        _db.SoxDuties.Remove(duty);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<SoxAssignmentDto>> GetAssignmentsAsync(Guid tenantId, string? userId = null, bool activeOnly = true)
    {
        var query = _db.SoxAssignments.Where(a => a.TenantId == tenantId);
        if (!string.IsNullOrEmpty(userId)) query = query.Where(a => a.UserId == userId);
        if (activeOnly) query = query.Where(a => a.IsActive);
        var assignments = await query.OrderByDescending(a => a.AssignedAt).ToListAsync();
        var duties = await _db.SoxDuties.Where(d => d.TenantId == tenantId).ToListAsync();
        var users = await _db.AdminUsers.ToListAsync();

        var dutyByCode = new Dictionary<string, SoxDutyEntity>(StringComparer.OrdinalIgnoreCase);
        foreach (var duty in duties) dutyByCode.TryAdd(duty.DutyCode, duty);

        return assignments.Select(assignment =>
        {
            dutyByCode.TryGetValue(assignment.DutyCode, out var duty);
            var user = users.FirstOrDefault(u => MatchesUser(assignment.UserId, u));
            return ToAssignmentDto(assignment, duty, user);
        }).ToList();
    }

    public async Task<DutyAssignmentResult> AssignDutyToUserAsync(Guid tenantId, SoxAssignRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return new DutyAssignmentResult { Success = false, Message = "UserId is required" };

        var duty = await _db.SoxDuties
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DutyCode == request.DutyCode && d.IsActive);
        if (duty == null)
            return new DutyAssignmentResult { Success = false, Message = $"Duty {request.DutyCode} not found or inactive" };

        var user = (await _db.AdminUsers.ToListAsync()).FirstOrDefault(u =>
            string.Equals(u.Id.ToString(), request.UserId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(u.UserId, request.UserId, StringComparison.OrdinalIgnoreCase));

        var assignment = new SoxAssignmentEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = request.UserId,
            UserName = user?.UserName ?? "",
            Role = user?.Role ?? "",
            DutyCode = duty.DutyCode,
            DutyName = duty.DutyName,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = string.IsNullOrWhiteSpace(request.AssignedBy) ? "system" : request.AssignedBy,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            Notes = request.Notes
        };
        _db.SoxAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        return new DutyAssignmentResult
        {
            Success = true,
            AssignmentId = assignment.Id,
            Message = "Duty assigned"
        };
    }

    public async Task<DutyRevokeResult> DeactivateAssignmentAsync(Guid tenantId, Guid assignmentId, string actor)
    {
        var assignment = await _db.SoxAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.TenantId == tenantId);
        if (assignment == null)
            return new DutyRevokeResult { Success = false, Message = "Assignment not found" };

        assignment.IsActive = false;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new DutyRevokeResult { Success = true, Message = "Assignment revoked" };
    }

    public async Task<SoDScanResult> RunSoDScanAsync(Guid tenantId, string scannedBy)
    {
        var now = DateTime.UtcNow;
        var duties = await _db.SoxDuties.Where(d => d.TenantId == tenantId && d.IsActive).ToListAsync();
        var users = await _db.AdminUsers.Where(u => u.IsActive).ToListAsync();
        var (activeCodes, denyRows, enforcedByCode) = await LoadAccessDataAsync();
        var tenantAssignments = await _db.SoxAssignments
            .Where(a => a.TenantId == tenantId && a.IsActive && (!a.ExpiresAt.HasValue || a.ExpiresAt > now))
            .ToListAsync();
        var existingOpen = await _db.SoxViolations
            .Where(v => v.TenantId == tenantId && v.Status == "Open")
            .ToListAsync();

        var dutyByCode = new Dictionary<string, SoxDutyEntity>(StringComparer.OrdinalIgnoreCase);
        foreach (var duty in duties) dutyByCode.TryAdd(duty.DutyCode, duty);

        var pairMap = new Dictionary<string, (string DutyA, string DutyB, string TransactionCode, string Severity)>(StringComparer.Ordinal);
        foreach (var duty in duties)
        {
            foreach (var conflictCode in ParseStringList(duty.ConflictDuties))
            {
                if (string.Equals(conflictCode, duty.DutyCode, StringComparison.OrdinalIgnoreCase)) continue;
                if (!dutyByCode.TryGetValue(conflictCode, out var other)) continue;

                var sorted = new[] { duty.DutyCode, conflictCode }.OrderBy(x => x, StringComparer.Ordinal).ToArray();
                var key = string.Join("|", sorted);
                if (pairMap.ContainsKey(key)) continue;

                var severity = IsHighRiskAction(duty.ActionType) || IsHighRiskAction(other.ActionType)
                    || duty.MinApprovers >= 2 || other.MinApprovers >= 2
                    ? "High"
                    : "Medium";
                var transactionCode = !string.IsNullOrEmpty(duty.TransactionCode) ? duty.TransactionCode : other.TransactionCode;
                pairMap[key] = (sorted[0], sorted[1], transactionCode, severity);
            }
        }

        var result = new SoDScanResult { ScannedAt = now };

        foreach (var user in users)
        {
            result.UsersScanned++;
            var allowed = ComputeAllowedCodes(user, activeCodes, denyRows, enforcedByCode);

            var held = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assignment in tenantAssignments)
            {
                if (MatchesUser(assignment.UserId, user)) held.Add(assignment.DutyCode);
            }
            foreach (var duty in duties)
            {
                if (string.IsNullOrWhiteSpace(duty.TransactionCode)) continue;
                if (allowed.Contains(duty.TransactionCode)) held.Add(duty.DutyCode);
            }
            result.DutiesEvaluated += held.Count;

            var seenPairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var duty in duties)
            {
                if (!held.Contains(duty.DutyCode)) continue;
                foreach (var conflictCode in ParseStringList(duty.ConflictDuties))
                {
                    if (!held.Contains(conflictCode)) continue;

                    var key = string.Join("|", new[] { duty.DutyCode, conflictCode }.OrderBy(x => x, StringComparer.Ordinal));
                    if (!pairMap.TryGetValue(key, out var pair)) continue;
                    if (!seenPairs.Add(key)) continue;

                    var alreadyOpen = existingOpen.Any(v =>
                        (string.Equals(v.UserId, user.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                            || string.Equals(v.UserName, user.UserName, StringComparison.OrdinalIgnoreCase))
                        && IsSamePair(v.DutyCode1, v.DutyCode2, pair.DutyA, pair.DutyB));
                    if (alreadyOpen)
                    {
                        result.ExistingSkipped++;
                        continue;
                    }

                    var violation = new SoxViolationEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        UserId = user.Id.ToString(),
                        UserName = user.UserName,
                        ViolationType = "SoD",
                        DutyCode1 = pair.DutyA,
                        DutyCode2 = pair.DutyB,
                        TransactionCode = pair.TransactionCode,
                        Severity = pair.Severity,
                        Status = "Open",
                        DetectedBy = scannedBy,
                        DetectedAt = now,
                        Description = $"User {user.UserName} holds conflicting duties {pair.DutyA} and {pair.DutyB}"
                    };
                    _db.SoxViolations.Add(violation);
                    existingOpen.Add(violation);
                    result.NewViolations++;
                    result.Violations.Add(new SoxDetectedViolation
                    {
                        ViolationId = violation.Id,
                        RuleCode = violation.ViolationType,
                        Description = violation.Description,
                        Severity = violation.Severity,
                        DetectedDate = violation.DetectedAt,
                        UserId = user.Id,
                        UserName = violation.UserName,
                        Status = violation.Status
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return result;
    }

    public async Task<List<SoxAssignmentDto>> GetEffectiveDutiesAsync(Guid tenantId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return new List<SoxAssignmentDto>();

        var user = (await _db.AdminUsers.ToListAsync()).FirstOrDefault(u =>
            string.Equals(u.Id.ToString(), userId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));
        if (user == null) return new List<SoxAssignmentDto>();

        var allDuties = await _db.SoxDuties.Where(d => d.TenantId == tenantId).ToListAsync();
        var assignments = await _db.SoxAssignments
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .ToListAsync();
        var (activeCodes, denyRows, enforcedByCode) = await LoadAccessDataAsync();

        var dutyByCode = new Dictionary<string, SoxDutyEntity>(StringComparer.OrdinalIgnoreCase);
        foreach (var duty in allDuties) dutyByCode.TryAdd(duty.DutyCode, duty);

        var result = new List<SoxAssignmentDto>();
        var explicitDutyCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assignment in assignments)
        {
            if (!MatchesUser(assignment.UserId, user)) continue;
            if (!explicitDutyCodes.Add(assignment.DutyCode)) continue;
            dutyByCode.TryGetValue(assignment.DutyCode, out var duty);
            result.Add(ToAssignmentDto(assignment, duty, user));
        }

        var allowed = ComputeAllowedCodes(user, activeCodes, denyRows, enforcedByCode);
        foreach (var duty in allDuties)
        {
            if (!duty.IsActive) continue;
            if (string.IsNullOrWhiteSpace(duty.TransactionCode)) continue;
            if (!allowed.Contains(duty.TransactionCode)) continue;
            if (explicitDutyCodes.Contains(duty.DutyCode)) continue;

            result.Add(new SoxAssignmentDto
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = user.UserName,
                Role = user.Role,
                DutyCode = duty.DutyCode,
                DutyName = duty.DutyName,
                AssignedAt = duty.CreatedAt,
                AssignedBy = "derived",
                ExpiresAt = null,
                IsActive = true,
                Notes = "",
                Source = "TCode"
            });
        }

        return result;
    }

    public async Task ExportViolationsToCsvAsync(Guid tenantId, Stream stream, string? status = null, string? severity = null)
    {
        var query = _db.SoxViolations.Where(v => v.TenantId == tenantId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(v => v.Status == status);
        if (!string.IsNullOrEmpty(severity)) query = query.Where(v => v.Severity == severity);
        var violations = await query.OrderByDescending(v => v.DetectedAt).ToListAsync();

        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteLineAsync("DetectedAt,User,Severity,Status,Duty1,Duty2,TransactionCode,Type,Description,ResolvedBy,ResolvedAt");
        foreach (var v in violations)
        {
            await writer.WriteLineAsync(string.Join(",",
                EscapeCsv(v.DetectedAt.ToString("O")),
                EscapeCsv(v.UserName),
                EscapeCsv(v.Severity),
                EscapeCsv(v.Status),
                EscapeCsv(v.DutyCode1),
                EscapeCsv(v.DutyCode2),
                EscapeCsv(v.TransactionCode),
                EscapeCsv(v.ViolationType),
                EscapeCsv(v.Description),
                EscapeCsv(v.ResolvedBy),
                EscapeCsv(v.ResolvedAt?.ToString("O"))));
        }
    }

    private async Task<(List<TransactionCodeEntity> ActiveCodes, List<TransactionPermissionEntity> DenyRows, Dictionary<string, string> EnforcedByCode)> LoadAccessDataAsync()
    {
        var activeCodes = await _db.TransactionCodes.Where(t => t.Status == "Active").ToListAsync();
        var denyRows = await _db.TransactionPermissions.Where(p => !p.CanAccess).ToListAsync();
        var enforcedByCode = (await _db.TCodeAuthChecks
                .Where(c => c.IsActive && c.Enforcement == "Enforced")
                .ToListAsync())
            .GroupBy(c => c.TCode.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => SecurityRoleRank.GetRank(c.RequiredRole)).First().RequiredRole, StringComparer.OrdinalIgnoreCase);
        return (activeCodes, denyRows, enforcedByCode);
    }

    private static HashSet<string> ComputeAllowedCodes(AdminUserEntity user, List<TransactionCodeEntity> activeCodes, List<TransactionPermissionEntity> denyRows, Dictionary<string, string> enforcedByCode)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (SecurityRoleRank.IsSuperUser(user.Role))
        {
            foreach (var code in activeCodes) allowed.Add(code.Code);
            return allowed;
        }

        foreach (var code in activeCodes)
        {
            if (!SecurityRoleRank.Meets(user.Role, code.RequiredRole)) continue;
            if (enforcedByCode.TryGetValue(code.Code, out var enforcedRole) && !SecurityRoleRank.Meets(user.Role, enforcedRole)) continue;
            if (IsDenied(code, user, denyRows)) continue;
            allowed.Add(code.Code);
        }
        return allowed;
    }

    private static bool IsDenied(TransactionCodeEntity code, AdminUserEntity user, List<TransactionPermissionEntity> denyRows)
    {
        foreach (var row in denyRows)
        {
            if (row.TransactionCodeId != code.Id) continue;
            if (string.Equals(row.PrincipalType, "Role", StringComparison.OrdinalIgnoreCase)
                && string.Equals(row.PrincipalValue, user.Role, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(row.PrincipalType, "User", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(row.PrincipalValue, user.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(row.PrincipalValue, user.UserId, StringComparison.OrdinalIgnoreCase))) return true;
        }
        return false;
    }

    private static bool MatchesUser(string userId, AdminUserEntity user) =>
        string.Equals(userId, user.Id.ToString(), StringComparison.OrdinalIgnoreCase)
        || string.Equals(userId, user.UserId, StringComparison.OrdinalIgnoreCase);

    private static bool IsHighRiskAction(string? actionType)
    {
        var action = (actionType ?? "").Trim().ToUpperInvariant();
        return action == "APPROVE" || action == "PAYMENT" || action == "ADMIN";
    }

    private static bool IsSamePair(string firstA, string firstB, string secondA, string secondB) =>
        (string.Equals(firstA, secondA, StringComparison.OrdinalIgnoreCase) && string.Equals(firstB, secondB, StringComparison.OrdinalIgnoreCase))
        || (string.Equals(firstA, secondB, StringComparison.OrdinalIgnoreCase) && string.Equals(firstB, secondA, StringComparison.OrdinalIgnoreCase));

    private static SoxDutyDto ToDutyDto(SoxDutyEntity duty) => new SoxDutyDto
    {
        Id = duty.Id,
        DutyCode = duty.DutyCode,
        DutyName = duty.DutyName,
        Description = duty.Description,
        Module = duty.Module,
        TransactionCode = duty.TransactionCode,
        ActionType = duty.ActionType,
        MinApprovers = duty.MinApprovers,
        RequiredRoles = ParseStringList(duty.RequiredRoles),
        ConflictDuties = ParseStringList(duty.ConflictDuties),
        IsActive = duty.IsActive,
        EffectiveFrom = duty.EffectiveFrom,
        EffectiveTo = duty.EffectiveTo
    };

    private static SoxAssignmentDto ToAssignmentDto(SoxAssignmentEntity assignment, SoxDutyEntity? duty, AdminUserEntity? user) => new SoxAssignmentDto
    {
        Id = assignment.Id,
        UserId = assignment.UserId,
        UserName = user?.UserName ?? assignment.UserName,
        Role = assignment.Role,
        DutyCode = assignment.DutyCode,
        DutyName = duty?.DutyName ?? assignment.DutyName,
        AssignedAt = assignment.AssignedAt,
        AssignedBy = assignment.AssignedBy,
        ExpiresAt = assignment.ExpiresAt,
        IsActive = assignment.IsActive,
        Notes = assignment.Notes,
        Source = "Explicit"
    };

    private static List<string> ParseStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static string ComputeSha256(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
