using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class WorkflowThresholdService : IWorkflowThresholdService
{
    private readonly YuktiraDbContext _db;

    public WorkflowThresholdService(YuktiraDbContext db) { _db = db; }

    public async Task<List<WorkflowThresholdDto>> ListAsync(Guid tenantId, string? documentType = null)
    {
        var query = _db.ReleaseStrategies.AsNoTracking().Where(s => s.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(documentType))
            query = query.Where(s => s.DocumentType == documentType);

        var strategies = await query.OrderBy(s => s.DocumentType).ThenBy(s => s.MinAmount).ToListAsync();
        var ids = strategies.Select(s => s.Id).ToList();
        var codes = ids.Count == 0
            ? new List<ReleaseCodeEntity>()
            : await _db.ReleaseCodes.AsNoTracking().Where(c => ids.Contains(c.ReleaseStrategyId)).ToListAsync();

        return strategies.Select(s => Map(s, codes.Where(c => c.ReleaseStrategyId == s.Id).ToList())).ToList();
    }

    public async Task<WorkflowThresholdDto?> GetAsync(Guid tenantId, Guid id)
    {
        var strategy = await _db.ReleaseStrategies.AsNoTracking().FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id);
        if (strategy == null) return null;

        var codes = await _db.ReleaseCodes.AsNoTracking().Where(c => c.ReleaseStrategyId == id).OrderBy(c => c.Level).ToListAsync();
        return Map(strategy, codes);
    }

    public async Task<WorkflowThresholdSaveResult> CreateAsync(Guid tenantId, WorkflowThresholdCreateRequest request)
    {
        var validation = await ValidateNoOverlapAsync(tenantId, request.DocumentType, request.MinAmount, request.MaxAmount);
        if (!validation.IsValid)
            return new WorkflowThresholdSaveResult { Success = false, Error = validation.Error };

        var documentType = request.DocumentType.Trim();
        var code = string.IsNullOrWhiteSpace(request.Code) ? BuildCode(documentType) : request.Code.Trim();
        var levels = ResolveLevels(request.Levels, request.ApproverRole);

        var entity = new ReleaseStrategyEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = string.IsNullOrWhiteSpace(request.Name) ? documentType + " approval threshold" : request.Name.Trim(),
            Description = request.Description ?? string.Empty,
            DocumentType = documentType,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            Plant = request.Plant ?? string.Empty,
            DepartmentKey = request.DepartmentKey ?? string.Empty,
            IsActive = request.IsActive
        };

        _db.ReleaseStrategies.Add(entity);
        foreach (var level in levels)
            _db.ReleaseCodes.Add(NewCode(tenantId, entity.Id, code, level));

        await _db.SaveChangesAsync();

        return new WorkflowThresholdSaveResult { Success = true, Threshold = Map(entity, levels) };
    }

    public async Task<WorkflowThresholdSaveResult> UpdateAsync(Guid tenantId, Guid id, WorkflowThresholdUpdateRequest request)
    {
        var entity = await _db.ReleaseStrategies.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id);
        if (entity == null)
            return new WorkflowThresholdSaveResult { Success = false, Error = "Approval threshold not found" };

        var validation = await ValidateNoOverlapAsync(tenantId, request.DocumentType, request.MinAmount, request.MaxAmount, id);
        if (!validation.IsValid)
            return new WorkflowThresholdSaveResult { Success = false, Error = validation.Error };

        entity.DocumentType = request.DocumentType.Trim();
        entity.Name = string.IsNullOrWhiteSpace(request.Name) ? entity.Name : request.Name.Trim();
        entity.Description = request.Description ?? entity.Description;
        entity.MinAmount = request.MinAmount;
        entity.MaxAmount = request.MaxAmount;
        entity.Plant = request.Plant ?? string.Empty;
        entity.DepartmentKey = request.DepartmentKey ?? string.Empty;
        entity.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Code)) entity.Code = request.Code.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        if (request.Levels != null)
        {
            var existingCodes = await _db.ReleaseCodes.Where(c => c.ReleaseStrategyId == id).ToListAsync();
            if (existingCodes.Count > 0) _db.ReleaseCodes.RemoveRange(existingCodes);

            var levels = ResolveLevels(request.Levels, request.ApproverRole);
            foreach (var level in levels)
                _db.ReleaseCodes.Add(NewCode(tenantId, id, entity.Code, level));
        }

        await _db.SaveChangesAsync();

        var savedLevels = await _db.ReleaseCodes.AsNoTracking().Where(c => c.ReleaseStrategyId == id).OrderBy(c => c.Level).ToListAsync();
        return new WorkflowThresholdSaveResult { Success = true, Threshold = Map(entity, savedLevels) };
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id)
    {
        var entity = await _db.ReleaseStrategies.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id);
        if (entity == null) return false;

        var codes = await _db.ReleaseCodes.Where(c => c.ReleaseStrategyId == id).ToListAsync();
        if (codes.Count > 0) _db.ReleaseCodes.RemoveRange(codes);

        _db.ReleaseStrategies.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<WorkflowThresholdDto?> PreviewAsync(Guid tenantId, string documentType, decimal amount, string plant = "", string departmentKey = "")
    {
        if (string.IsNullOrWhiteSpace(documentType)) return null;

        var match = await _db.ReleaseStrategies.AsNoTracking().FirstOrDefaultAsync(s =>
            s.TenantId == tenantId &&
            s.IsActive &&
            s.DocumentType == documentType &&
            amount >= s.MinAmount &&
            amount <= s.MaxAmount &&
            (string.IsNullOrEmpty(s.Plant) || s.Plant == plant) &&
            (string.IsNullOrEmpty(s.DepartmentKey) || s.DepartmentKey == departmentKey));

        if (match == null) return null;

        var codes = await _db.ReleaseCodes.AsNoTracking().Where(c => c.ReleaseStrategyId == match.Id).OrderBy(c => c.Level).ToListAsync();
        return Map(match, codes);
    }

    public async Task<WorkflowThresholdValidationResult> ValidateNoOverlapAsync(Guid tenantId, string documentType, decimal minAmount, decimal maxAmount, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(documentType))
            return new WorkflowThresholdValidationResult { IsValid = false, Error = "Document type is required" };

        if (minAmount > maxAmount)
            return new WorkflowThresholdValidationResult { IsValid = false, Error = "Minimum amount cannot be greater than maximum amount" };

        var query = _db.ReleaseStrategies.AsNoTracking().Where(s => s.TenantId == tenantId && s.DocumentType == documentType);
        if (excludeId.HasValue) query = query.Where(s => s.Id != excludeId.Value);

        var existing = await query.OrderBy(s => s.MinAmount).ToListAsync();
        var overlap = existing.FirstOrDefault(s => s.MinAmount < maxAmount && s.MaxAmount > minAmount);
        if (overlap == null)
            return new WorkflowThresholdValidationResult { IsValid = true, Error = string.Empty };

        return new WorkflowThresholdValidationResult
        {
            IsValid = false,
            Error = $"Amount range {minAmount} - {maxAmount} for {documentType} overlaps threshold {overlap.Code} ({overlap.MinAmount} - {overlap.MaxAmount})"
        };
    }

    private static List<WorkflowThresholdLevelDto> ResolveLevels(List<WorkflowThresholdLevelDto>? levels, string approverRole)
    {
        var resolved = new List<WorkflowThresholdLevelDto>();
        var order = 1;

        if (levels != null && levels.Count > 0)
        {
            foreach (var level in levels.Where(l => l != null).OrderBy(l => l.Level))
                resolved.Add(new WorkflowThresholdLevelDto { Level = order++, ApproverRole = (level.ApproverRole ?? string.Empty).Trim() });
        }
        else if (!string.IsNullOrWhiteSpace(approverRole))
        {
            var roles = approverRole.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var role in roles)
                resolved.Add(new WorkflowThresholdLevelDto { Level = order++, ApproverRole = role });
        }

        if (resolved.Count == 0)
            resolved.Add(new WorkflowThresholdLevelDto { Level = 1, ApproverRole = "ADMIN" });

        return resolved;
    }

    private static ReleaseCodeEntity NewCode(Guid tenantId, Guid strategyId, string code, WorkflowThresholdLevelDto level) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        ReleaseStrategyId = strategyId,
        Level = level.Level,
        Code = $"{code}-L{level.Level}",
        ApproverRole = level.ApproverRole,
        ApproverUserId = string.Empty,
        IsRequired = true
    };

    private static string BuildCode(string documentType)
    {
        var prefix = string.IsNullOrWhiteSpace(documentType) ? "RS" : documentType.Trim().ToUpperInvariant();
        return $"{prefix}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private static WorkflowThresholdDto Map(ReleaseStrategyEntity entity, List<ReleaseCodeEntity> codes) =>
        Map(entity, codes.OrderBy(c => c.Level).Select(c => new WorkflowThresholdLevelDto { Level = c.Level, ApproverRole = c.ApproverRole }).ToList());

    private static WorkflowThresholdDto Map(ReleaseStrategyEntity entity, List<WorkflowThresholdLevelDto> levels) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        DocumentType = entity.DocumentType,
        MinAmount = entity.MinAmount,
        MaxAmount = entity.MaxAmount,
        Plant = entity.Plant,
        DepartmentKey = entity.DepartmentKey,
        IsActive = entity.IsActive,
        Levels = levels,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
