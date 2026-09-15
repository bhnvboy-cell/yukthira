using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

        if (_workflow != null)
        {
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
                        new Dictionary<string, object> { ["ChangeRequestId"] = entity.Id, ["EntityName"] = request.EntityName });
                    entity.WorkflowInstanceId = instanceId;
                    await _db.SaveChangesAsync();
                }
            }
            catch { }
        }

        return MapToDto(entity);
    }

    public async Task<MdgChangeRequestDto> ApproveChangeRequestAsync(Guid requestId, MdgApprovalRequest approval, Guid tenantId)
    {
        var entity = await GetEntityAsync(requestId, tenantId)
            ?? throw new InvalidOperationException("Change request not found.");

        if (entity.Status != "PendingApproval")
            throw new InvalidOperationException($"Cannot approve request in '{entity.Status}' status.");

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
        return MapToDto(entity);
    }

    public async Task<MdgChangeRequestDto> RejectChangeRequestAsync(Guid requestId, MdgRejectionRequest rejection, Guid tenantId)
    {
        var entity = await GetEntityAsync(requestId, tenantId)
            ?? throw new InvalidOperationException("Change request not found.");

        if (entity.Status != "PendingApproval")
            throw new InvalidOperationException($"Cannot reject request in '{entity.Status}' status.");

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

    private static MdgChangeRequestDto MapToDtoQuery(MdgChangeRequestEntity e)
    {
        return MapToDto(e);
    }
}
