using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class MicService : IMicService
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public MicService(YuktiraDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<MicDto> CreateMicAsync(MicCreateRequest request, Guid tenantId)
    {
        if (await CheckMicExistsAsync(request.PlantId, request.CharacteristicCode, tenantId))
            throw new DuplicateEntityException("MIC", $"{request.PlantId}/{request.CharacteristicCode}");

        var entity = new MicMasterEntity
        {
            TenantId = tenantId,
            PlantId = request.PlantId,
            CharacteristicCode = request.CharacteristicCode,
            ValidFrom = DateTime.UtcNow,
            IsQuantitative = request.IsQuantitative,
            ShortText = request.ShortText,
            Status = "BeingCreated",
            LowerSpecLimit = request.LowerSpecLimit,
            UpperSpecLimit = request.UpperSpecLimit,
            TargetValueRequired = request.TargetValueRequired,
            ResultsConfirmation = request.ResultsConfirmation,
            Requirement = request.Requirement,
            DecimalPlaces = request.DecimalPlaces,
            LowerTolerance = request.LowerTolerance,
            UpperTolerance = request.UpperTolerance,
            TargetValue = request.TargetValue
        };

        _db.MicMasters.Add(entity);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            TenantId = tenantId,
            ModuleName = "QM",
            ActionType = YuktiraERP.Core.Domain.Common.ActionType.Create,
            EntityName = "MicMaster",
            EntityId = entity.Id.ToString(),
            NewValue = $"MIC created: {entity.PlantId}/{entity.CharacteristicCode}",
            Details = $"MIC Master {entity.CharacteristicCode} created for plant {entity.PlantId}"
        });

        return ToDto(entity);
    }

    public async Task<List<MicDto>> SearchMicsAsync(MicSearchFilter filter, Guid tenantId)
    {
        var query = _db.MicMasters
            .Where(m => m.TenantId == tenantId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.PlantId))
            query = query.Where(m => m.PlantId == filter.PlantId);

        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(m => m.Status == filter.Status);

        if (!string.IsNullOrEmpty(filter.CharacteristicCode))
            query = query.Where(m => m.CharacteristicCode.Contains(filter.CharacteristicCode));

        if (filter.IsQuantitative.HasValue)
            query = query.Where(m => m.IsQuantitative == filter.IsQuantitative.Value);

        var entities = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return entities.Select(ToDto).ToList();
    }

    public async Task<bool> CheckMicExistsAsync(string plantId, string characteristicCode, Guid tenantId)
    {
        return await _db.MicMasters
            .AnyAsync(m => m.TenantId == tenantId
                && m.PlantId == plantId
                && m.CharacteristicCode == characteristicCode);
    }

    public async Task<MicDto?> GetMicByIdAsync(Guid id, Guid tenantId)
    {
        var entity = await _db.MicMasters
            .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId);

        return entity == null ? null : ToDto(entity);
    }

    private static MicDto ToDto(MicMasterEntity entity) => new()
    {
        Id = entity.Id,
        TenantId = entity.TenantId,
        PlantId = entity.PlantId,
        CharacteristicCode = entity.CharacteristicCode,
        ValidFrom = entity.ValidFrom,
        IsQuantitative = entity.IsQuantitative,
        ShortText = entity.ShortText,
        Status = entity.Status,
        LowerSpecLimit = entity.LowerSpecLimit,
        UpperSpecLimit = entity.UpperSpecLimit,
        TargetValueRequired = entity.TargetValueRequired,
        ResultsConfirmation = entity.ResultsConfirmation,
        Requirement = entity.Requirement,
        DecimalPlaces = entity.DecimalPlaces,
        LowerTolerance = entity.LowerTolerance,
        UpperTolerance = entity.UpperTolerance,
        TargetValue = entity.TargetValue,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
