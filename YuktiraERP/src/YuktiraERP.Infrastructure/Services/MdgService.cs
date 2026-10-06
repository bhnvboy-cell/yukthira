using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class MdgService : IMdgService
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;
    private readonly IWorkflowEngine? _workflow;

    public MdgService(YuktiraDbContext db, ITenantContext tenant, IAuditService audit, IWorkflowEngine? workflow = null)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
        _workflow = workflow;
    }

    public async Task<MdgChangeRequestDto> SubmitChangeRequestAsync(MdgSubmitRequest request, Guid tenantId)
    {
        var validation = await ValidateStagingPayloadAsync(request.EntityName, request.StagingPayload, tenantId);
        if (!validation.IsValid)
            throw new InvalidOperationException($"Validation failed: {string.Join("; ", validation.Errors)}");

        var existing = await _db.MdgChangeRequests
            .Where(cr => cr.TenantId == tenantId
                && cr.EntityName == request.EntityName
                && cr.Status != "Rejected"
                && cr.StagingPayload == request.StagingPayload)
            .FirstOrDefaultAsync();
        if (existing != null)
            throw new InvalidOperationException($"Duplicate staging payload detected for {request.EntityName}. Existing request: {existing.RequestNumber}");

        var requestNumber = await GenerateRequestNumberAsync(tenantId);
        var hash = ComputeSha256(request.StagingPayload);

        var entity = new MdgChangeRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequestNumber = requestNumber,
            EntityName = request.EntityName,
            StagingPayload = request.StagingPayload,
            Status = "PendingApproval",
            RequestedBy = request.RequestedBy,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.MdgChangeRequests.Add(entity);

        var auditLog = new MdgAuditLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChangeRequestId = entity.Id,
            Action = "SUBMITTED",
            Actor = request.RequestedBy,
            Timestamp = DateTime.UtcNow,
            NewValue = request.StagingPayload,
            HashSha256 = hash,
            CreatedAt = DateTime.UtcNow
        };
        _db.MdgAuditLogs.Add(auditLog);

        await _db.SaveChangesAsync();

        await StartWorkflowForRequestAsync(entity, tenantId);

        await _audit.LogAsync(BuildSoxAudit(entity, "Submit", ActionType.Workflow, null, "PendingApproval", null, request.RequestedBy));

        return MapToDto(entity);
    }

    public async Task<MdgChangeRequestDto> ApproveChangeRequestAsync(Guid requestId, MdgApprovalRequest approval, Guid tenantId)
    {
        if (approval == null || string.IsNullOrWhiteSpace(approval.Notes))
            throw new InvalidOperationException("An approval comment (Notes) is required: SOX controls mandate a documented reason for every approval.");

        var entity = await GetEntityAsync(requestId, tenantId)
            ?? throw new InvalidOperationException("Change request not found.");

        if (entity.Status != "PendingApproval")
            throw new InvalidOperationException($"Cannot approve request in '{entity.Status}' status.");

        var previousStatus = entity.Status;
        entity.Status = "Approved";
        entity.ApprovedBy = approval.ApprovedBy;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        var auditLog = new MdgAuditLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChangeRequestId = entity.Id,
            Action = "APPROVED",
            Actor = approval.ApprovedBy,
            Timestamp = DateTime.UtcNow,
            OldValue = "PendingApproval",
            NewValue = "Approved",
            HashSha256 = ComputeSha256(approval.Notes ?? ""),
            CreatedAt = DateTime.UtcNow
        };
        _db.MdgAuditLogs.Add(auditLog);

        await _db.SaveChangesAsync();

        await _audit.LogAsync(BuildSoxAudit(entity, "Approve", ActionType.Approval, previousStatus, "Approved", approval.Notes, approval.ApprovedBy));

        return MapToDto(entity);
    }

    public async Task<MdgChangeRequestDto> RejectChangeRequestAsync(Guid requestId, MdgRejectionRequest rejection, Guid tenantId)
    {
        if (rejection == null || string.IsNullOrWhiteSpace(rejection.Reason))
            throw new InvalidOperationException("A rejection comment (Reason) is required: SOX controls mandate a documented reason for every rejection.");

        var entity = await GetEntityAsync(requestId, tenantId)
            ?? throw new InvalidOperationException("Change request not found.");

        if (entity.Status != "PendingApproval")
            throw new InvalidOperationException($"Cannot reject request in '{entity.Status}' status.");

        var previousStatus = entity.Status;
        entity.Status = "Rejected";
        entity.RejectionReason = rejection.Reason;
        entity.UpdatedAt = DateTime.UtcNow;

        var auditLog = new MdgAuditLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChangeRequestId = entity.Id,
            Action = "REJECTED",
            Actor = rejection.RejectedBy,
            Timestamp = DateTime.UtcNow,
            OldValue = "PendingApproval",
            NewValue = "Rejected",
            HashSha256 = ComputeSha256(rejection.Reason),
            CreatedAt = DateTime.UtcNow
        };
        _db.MdgAuditLogs.Add(auditLog);

        await _db.SaveChangesAsync();

        await _audit.LogAsync(BuildSoxAudit(entity, "Reject", ActionType.Approval, previousStatus, "Rejected", rejection.Reason, rejection.RejectedBy));

        return MapToDto(entity);
    }

    public async Task<MdgChangeRequestDto> ActivateChangeRequestAsync(Guid requestId, Guid tenantId)
    {
        var entity = await GetEntityAsync(requestId, tenantId)
            ?? throw new InvalidOperationException("Change request not found.");

        if (entity.Status != "Approved")
            throw new InvalidOperationException($"Cannot activate request in '{entity.Status}' status. Must be Approved.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var payloadDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entity.StagingPayload)
                ?? throw new InvalidOperationException("Invalid JSON payload.");

            var entityType = FindEntityType(entity.EntityName);
            if (entityType == null)
                throw new InvalidOperationException($"Unknown entity type: {entity.EntityName}");

            var dbSetProperty = _db.GetType().GetProperties()
                .FirstOrDefault(p => p.PropertyType.IsGenericType
                    && p.PropertyType.GetGenericArguments()[0] == entityType);
            if (dbSetProperty == null)
                throw new InvalidOperationException($"No DbSet found for entity type: {entity.EntityName}");

            var dbSetValue = dbSetProperty.GetValue(_db);
            if (dbSetValue == null)
                throw new InvalidOperationException($"DbSet for {entity.EntityName} is null.");

            var addMethod = dbSetValue.GetType().GetMethod("Add")!;
            var newEntity = Activator.CreateInstance(entityType)!;

            foreach (var prop in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.Name == "Id" || prop.Name == "CreatedAt" || prop.Name == "UpdatedAt")
                    continue;

                if (payloadDict.TryGetValue(prop.Name, out var jsonElement))
                {
                    var value = JsonSerializer.Deserialize(jsonElement.GetRawText(), prop.PropertyType,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (value != null && prop.CanWrite)
                        prop.SetValue(newEntity, value);
                }
            }

            if (newEntity is EntityBase baseEntity)
            {
                baseEntity.Id = Guid.NewGuid();
                baseEntity.CreatedAt = DateTime.UtcNow;
            }

            var entityIdValue = entityType.GetProperty("Id")?.GetValue(newEntity)?.ToString();
            entity.EntityId = entityIdValue;

            addMethod.Invoke(dbSetValue, new object[] { newEntity });
            await _db.SaveChangesAsync();

            entity.Status = "Activated";
            entity.ActivatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var auditLog = new MdgAuditLogEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ChangeRequestId = entity.Id,
                Action = "ACTIVATED",
                Actor = entity.ApprovedBy ?? "System",
                Timestamp = DateTime.UtcNow,
                OldValue = "Approved",
                NewValue = "Activated",
                HashSha256 = ComputeSha256(entity.StagingPayload),
                CreatedAt = DateTime.UtcNow
            };
            _db.MdgAuditLogs.Add(auditLog);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        await _audit.LogAsync(BuildSoxAudit(entity, "Activate", ActionType.Create, "Approved", "Activated", null, entity.ApprovedBy));

        return MapToDto(entity);
    }

    public async Task<List<MdgChangeRequestDto>> GetPendingRequestsAsync(Guid tenantId)
    {
        var entities = await _db.MdgChangeRequests
            .Where(cr => cr.TenantId == tenantId && cr.Status == "PendingApproval")
            .OrderByDescending(cr => cr.SubmittedAt)
            .ToListAsync();
        return entities.Select(MapToDto).ToList();
    }

    public async Task<MdgChangeRequestDto?> GetChangeRequestAsync(Guid requestId, Guid tenantId)
    {
        var entity = await GetEntityAsync(requestId, tenantId);
        return entity == null ? null : MapToDto(entity);
    }

    public Task<MdgValidationResult> ValidateStagingPayloadAsync(string entityName, string payload, Guid tenantId)
    {
        var result = new MdgValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(entityName))
        {
            result.Errors.Add("EntityName is required.");
            result.IsValid = false;
            return Task.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            result.Errors.Add("StagingPayload is required.");
            result.IsValid = false;
            return Task.FromResult(result);
        }

        try
        {
            var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                result.Errors.Add("StagingPayload must be a JSON object.");
                result.IsValid = false;
                return Task.FromResult(result);
            }
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"Invalid JSON payload: {ex.Message}");
            result.IsValid = false;
            return Task.FromResult(result);
        }

        var entityType = FindEntityType(entityName);
        if (entityType == null)
        {
            result.Errors.Add($"Unknown entity type: {entityName}");
            result.IsValid = false;
            return Task.FromResult(result);
        }

        try
        {
            var payloadDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payload);
            if (payloadDict == null)
            {
                result.Errors.Add("Could not deserialize payload.");
                result.IsValid = false;
                return Task.FromResult(result);
            }

            var requiredFields = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
                    && !p.Name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase)
                    && !p.Name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase)
                    && p.PropertyType != typeof(Guid?)
                    && p.PropertyType != typeof(DateTime?)
                    && !p.PropertyType.IsGenericType
                    && p.PropertyType != typeof(List<>).MakeGenericType(p.PropertyType.GetGenericArguments().FirstOrDefault() ?? typeof(object)))
                .Select(p => p.Name)
                .ToList();

            var hasRequired = payloadDict.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var field in requiredFields.Where(f => !f.Equals("Id", StringComparison.OrdinalIgnoreCase)))
            {
                if (!hasRequired.Contains(field))
                    result.Warnings.Add($"Field '{field}' is not present in the payload.");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Payload validation error: {ex.Message}");
            result.IsValid = false;
        }

        return Task.FromResult(result);
    }

    public async Task<MdgPagedResult<MdgChangeRequestDto>> ListRequestsAsync(string? status, string? entityName, string? search, int page, int pageSize)
    {
        var tenantId = _tenant.TenantId;
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var query = _db.MdgChangeRequests.Where(cr => cr.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusFilter = status.Trim();
            query = query.Where(cr => cr.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            var entityFilter = entityName.Trim();
            query = query.Where(cr => cr.EntityName == entityFilter);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(cr => cr.RequestNumber.Contains(term)
                || cr.EntityName.Contains(term)
                || cr.RequestedBy.Contains(term));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new MdgPagedResult<MdgChangeRequestDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MdgChangeRequestDto?> GetRequestAsync(Guid id)
    {
        var entity = await GetEntityAsync(id, _tenant.TenantId);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<MdgDiffDto?> GetDiffAsync(Guid id)
    {
        var request = await GetEntityAsync(id, _tenant.TenantId);
        if (request == null) return null;

        var originalJson = await ResolveOriginalJsonAsync(request);
        var proposedJson = string.IsNullOrWhiteSpace(request.StagingPayload) ? "{}" : request.StagingPayload;

        return new MdgDiffDto
        {
            OriginalJson = originalJson,
            ProposedJson = proposedJson,
            Changes = ComputeChanges(originalJson, proposedJson)
        };
    }

    public async Task<List<MdgAuditTrailDto>?> GetAuditTrailAsync(Guid requestId)
    {
        var tenantId = _tenant.TenantId;
        var request = await GetEntityAsync(requestId, tenantId);
        if (request == null) return null;

        var rows = await _db.MdgAuditLogs
            .Where(a => a.TenantId == tenantId && a.ChangeRequestId == requestId)
            .OrderBy(a => a.Timestamp)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync();

        var trail = new List<MdgAuditTrailDto>(rows.Count);
        string? previousHash = null;
        foreach (var row in rows)
        {
            trail.Add(new MdgAuditTrailDto
            {
                Id = row.Id,
                ChangeRequestId = row.ChangeRequestId,
                Action = row.Action,
                Actor = row.Actor,
                Timestamp = row.Timestamp,
                OldValue = row.OldValue,
                NewValue = row.NewValue,
                Comment = ResolveAuditComment(request, row),
                Hash = row.HashSha256,
                PreviousHash = previousHash,
                CreatedAt = row.CreatedAt
            });
            previousHash = row.HashSha256;
        }

        return trail;
    }

    public async Task<MdgChangeRequestDto> CreateDraftAsync(MdgCreateDraftRequest request)
    {
        if (request == null)
            throw new InvalidOperationException("Draft payload is required.");

        var tenantId = _tenant.TenantId;

        var validation = await ValidateStagingPayloadAsync(request.EntityName, request.StagingPayload, tenantId);
        if (!validation.IsValid)
            throw new InvalidOperationException($"Validation failed: {string.Join("; ", validation.Errors)}");

        var existing = await _db.MdgChangeRequests
            .Where(cr => cr.TenantId == tenantId
                && cr.EntityName == request.EntityName
                && cr.Status != "Rejected"
                && cr.StagingPayload == request.StagingPayload)
            .FirstOrDefaultAsync();
        if (existing != null)
            throw new InvalidOperationException($"Duplicate staging payload detected for {request.EntityName}. Existing request: {existing.RequestNumber}");

        var requestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "unknown" : request.RequestedBy.Trim();
        var entity = new MdgChangeRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RequestNumber = await GenerateRequestNumberAsync(tenantId),
            EntityName = request.EntityName,
            StagingPayload = request.StagingPayload,
            Status = "Draft",
            RequestedBy = requestedBy,
            SubmittedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        _db.MdgChangeRequests.Add(entity);
        _db.MdgAuditLogs.Add(new MdgAuditLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChangeRequestId = entity.Id,
            Action = "DRAFT_CREATED",
            Actor = requestedBy,
            Timestamp = DateTime.UtcNow,
            OldValue = request.Notes,
            NewValue = "Draft",
            HashSha256 = ComputeSha256(request.StagingPayload),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await _audit.LogAsync(BuildSoxAudit(entity, "DraftCreate", ActionType.Create, null, "Draft", request.Notes, requestedBy));

        return MapToDto(entity);
    }

    public async Task<MdgChangeRequestDto> SubmitAsync(Guid id)
    {
        var tenantId = _tenant.TenantId;
        var entity = await GetEntityAsync(id, tenantId)
            ?? throw new InvalidOperationException("Change request not found.");

        if (entity.Status != "Draft")
            throw new InvalidOperationException($"Cannot submit request in '{entity.Status}' status. Only Draft requests can be submitted.");

        var validation = await ValidateStagingPayloadAsync(entity.EntityName, entity.StagingPayload, tenantId);
        if (!validation.IsValid)
            throw new InvalidOperationException($"Validation failed: {string.Join("; ", validation.Errors)}");

        var duplicate = await _db.MdgChangeRequests
            .Where(cr => cr.TenantId == tenantId
                && cr.Id != entity.Id
                && cr.EntityName == entity.EntityName
                && cr.Status != "Rejected"
                && cr.StagingPayload == entity.StagingPayload)
            .FirstOrDefaultAsync();
        if (duplicate != null)
            throw new InvalidOperationException($"Duplicate staging payload detected for {entity.EntityName}. Existing request: {duplicate.RequestNumber}");

        var previousStatus = entity.Status;
        entity.Status = "PendingApproval";
        entity.SubmittedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        _db.MdgAuditLogs.Add(new MdgAuditLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChangeRequestId = entity.Id,
            Action = "SUBMITTED",
            Actor = entity.RequestedBy,
            Timestamp = DateTime.UtcNow,
            OldValue = previousStatus,
            NewValue = "PendingApproval",
            HashSha256 = ComputeSha256(entity.StagingPayload),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await StartWorkflowForRequestAsync(entity, tenantId);

        await _audit.LogAsync(BuildSoxAudit(entity, "Submit", ActionType.Workflow, previousStatus, "PendingApproval", null, entity.RequestedBy));

        return MapToDto(entity);
    }

    private async Task<MdgChangeRequestEntity?> GetEntityAsync(Guid requestId, Guid tenantId)
    {
        return await _db.MdgChangeRequests
            .FirstOrDefaultAsync(cr => cr.Id == requestId && cr.TenantId == tenantId);
    }

    private async Task<string> GenerateRequestNumberAsync(Guid tenantId)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"MDG-{today}-";
        var count = await _db.MdgChangeRequests
            .Where(cr => cr.TenantId == tenantId && cr.RequestNumber.StartsWith(prefix))
            .CountAsync();
        return $"{prefix}{(count + 1):D4}";
    }

    private async Task StartWorkflowForRequestAsync(MdgChangeRequestEntity entity, Guid tenantId)
    {
        if (_workflow == null) return;

        try
        {
            var workflowDef = await _db.WorkflowDefinitions
                .Where(w => w.TenantId == tenantId && w.Module == "MDG" && w.IsActive)
                .FirstOrDefaultAsync();
            if (workflowDef != null)
            {
                var instanceId = await _workflow.StartWorkflowAsync(
                    workflowDef.Id, tenantId, "MdgChangeRequest",
                    entity.Id.ToString(), Guid.Empty,
                    new Dictionary<string, object> { ["ChangeRequestId"] = entity.Id, ["EntityName"] = entity.EntityName });
                entity.WorkflowInstanceId = instanceId;
                await _db.SaveChangesAsync();
            }
        }
        catch { }
    }

    private static AuditEntryDto BuildSoxAudit(MdgChangeRequestEntity entity, string action, ActionType actionType, string? oldValue, string? newValue, string? comment, string? actor)
    {
        return new AuditEntryDto
        {
            Timestamp = DateTime.UtcNow,
            UserId = Guid.TryParse(actor, out var actorId) ? actorId : null,
            TenantId = entity.TenantId,
            ModuleName = "MDG",
            ActionType = actionType,
            EntityName = "ChangeRequest",
            EntityId = entity.Id.ToString(),
            OldValue = oldValue,
            NewValue = newValue,
            Details = string.IsNullOrWhiteSpace(comment)
                ? $"{action} for {entity.RequestNumber} ({entity.EntityName})"
                : comment
        };
    }

    private static string ResolveAuditComment(MdgChangeRequestEntity request, MdgAuditLogEntity row)
    {
        if (row.Action == "REJECTED" && !string.IsNullOrWhiteSpace(request.RejectionReason))
            return request.RejectionReason;

        if (row.Action == "DRAFT_CREATED" && !string.IsNullOrWhiteSpace(row.OldValue))
            return row.OldValue;

        return "";
    }

    private async Task<string> ResolveOriginalJsonAsync(MdgChangeRequestEntity request)
    {
        if (string.IsNullOrWhiteSpace(request.EntityId) || !Guid.TryParse(request.EntityId, out var entityId))
            return "{}";

        var entityType = FindEntityType(request.EntityName);
        if (entityType == null || _db.Model.FindEntityType(entityType) == null)
            return "{}";

        object? record;
        try
        {
            record = await _db.FindAsync(entityType, entityId);
        }
        catch (InvalidOperationException)
        {
            return "{}";
        }

        if (record == null)
            return "{}";

        try
        {
            return JsonSerializer.Serialize(record, record.GetType());
        }
        catch (InvalidOperationException)
        {
            return "{}";
        }
        catch (NotSupportedException)
        {
            return "{}";
        }
    }

    private static List<MdgFieldChangeDto> ComputeChanges(string originalJson, string proposedJson)
    {
        var original = ParseJsonObject(originalJson) ?? new JsonObject();
        var proposed = ParseJsonObject(proposedJson) ?? new JsonObject();

        var changes = new List<MdgFieldChangeDto>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in original)
        {
            visited.Add(property.Key);
            AppendChange(changes, property.Key, original, proposed);
        }

        foreach (var property in proposed)
        {
            if (visited.Contains(property.Key)) continue;
            AppendChange(changes, property.Key, original, proposed);
        }

        return changes;
    }

    private static void AppendChange(List<MdgFieldChangeDto> changes, string field, JsonObject original, JsonObject proposed)
    {
        var hasOriginal = TryGetJson(original, field, out var fromNode);
        var hasProposed = TryGetJson(proposed, field, out var toNode);

        var from = hasOriginal ? NodeToText(fromNode) : null;
        var to = hasProposed ? NodeToText(toNode) : null;

        if (hasOriginal && hasProposed)
        {
            if (string.Equals(from, to, StringComparison.Ordinal)) return;
            changes.Add(new MdgFieldChangeDto { Field = field, From = from, To = to, ChangeType = "Modified" });
        }
        else if (hasOriginal)
        {
            if (from == null) return;
            changes.Add(new MdgFieldChangeDto { Field = field, From = from, To = null, ChangeType = "Removed" });
        }
        else
        {
            if (to == null) return;
            changes.Add(new MdgFieldChangeDto { Field = field, From = null, To = to, ChangeType = "Added" });
        }
    }

    private static bool TryGetJson(JsonObject obj, string key, out JsonNode? value)
    {
        foreach (var pair in obj)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static JsonObject? ParseJsonObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? NodeToText(JsonNode? node)
    {
        if (node == null) return null;
        if (node is JsonValue scalar && scalar.TryGetValue<string>(out var text)) return text;
        return node.ToJsonString();
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(64);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static Type? FindEntityType(string entityName)
    {
        var coreAssembly = typeof(EntityBase).Assembly;
        var infraAssembly = typeof(MdgService).Assembly;
        var allTypes = Enumerable.Empty<Type>();
        foreach (var asm in new[] { coreAssembly, infraAssembly })
        {
            try { allTypes = allTypes.Concat(asm.GetTypes()); }
            catch { }
        }
        return allTypes.FirstOrDefault(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(EntityBase))
                && (string.Equals(t.Name, entityName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t.Name, entityName + "Entity", StringComparison.OrdinalIgnoreCase)));
    }

    private static MdgChangeRequestDto MapToDto(MdgChangeRequestEntity e)
    {
        return new MdgChangeRequestDto
        {
            Id = e.Id,
            TenantId = e.TenantId,
            RequestNumber = e.RequestNumber,
            EntityName = e.EntityName,
            EntityId = e.EntityId,
            StagingPayload = e.StagingPayload,
            Status = e.Status,
            RequestedBy = e.RequestedBy,
            ApprovedBy = e.ApprovedBy,
            SubmittedAt = e.SubmittedAt,
            ApprovedAt = e.ApprovedAt,
            ActivatedAt = e.ActivatedAt,
            RejectionReason = e.RejectionReason,
            WorkflowInstanceId = e.WorkflowInstanceId,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
