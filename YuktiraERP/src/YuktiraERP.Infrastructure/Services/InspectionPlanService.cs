using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class InspectionPlanService : IInspectionPlanService
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;

    public InspectionPlanService(YuktiraDbContext db, ITenantContext tenant, IAuditService audit)
    {
        _db = db;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<InspectionPlanDto> CreateGenericPlanAsync(InspectionPlanCreateRequest request, Guid tenantId)
    {
        var groupKey = Guid.NewGuid().ToString("N");

        var header = new QmInspectionPlanHeaderEntity
        {
            TenantId = tenantId,
            GroupKey = groupKey,
            GroupCounter = 1,
            PlantId = request.PlantId,
            MaterialId = request.MaterialId,
            Usage = request.Usage,
            OverallStatus = "4",
            LotSizeFrom = request.LotSizeFrom,
            LotSizeTo = request.LotSizeTo,
            Description = request.Description,
            ValidFrom = DateTime.UtcNow,
            ValidTo = null
        };

        _db.QmInspectionPlanHeaders.Add(header);

        var operation = new QmInspectionPlanOperationEntity
        {
            TenantId = tenantId,
            PlanHeaderId = header.Id,
            OperationNo = "0010",
            OperationDescription = "Standard Inspection",
            WorkCenter = null,
            BaseQuantity = 1
        };

        _db.QmInspectionPlanOperations.Add(operation);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            TenantId = tenantId,
            ModuleName = "QM",
            ActionType = YuktiraERP.Core.Domain.Common.ActionType.Create,
            EntityName = "QmInspectionPlanHeader",
            EntityId = header.Id.ToString(),
            NewValue = System.Text.Json.JsonSerializer.Serialize(new { header.GroupKey, header.PlantId, header.MaterialId })
        });

        return await MapToDtoAsync(header.Id, tenantId);
    }

    public async Task<InspectionPlanDto> AssignMicToOperationAsync(Guid planId, InspectionPlanMicAssignRequest request, Guid tenantId)
    {
        var header = await _db.QmInspectionPlanHeaders
            .FirstOrDefaultAsync(h => h.Id == planId && h.TenantId == tenantId)
            ?? throw new InvalidOperationException($"Inspection plan {planId} not found.");

        var operation = await _db.QmInspectionPlanOperations
            .FirstOrDefaultAsync(o => o.Id == request.OperationId && o.PlanHeaderId == planId && o.TenantId == tenantId)
            ?? throw new InvalidOperationException($"Operation {request.OperationId} not found in plan {planId}.");

        var micExists = await _db.Set<MicMasterEntity>().AnyAsync(m =>
            m.CharacteristicCode == request.MicCode &&
            m.PlantId == request.MicPlantId &&
            m.TenantId == tenantId);

        if (!micExists)
            throw new InvalidOperationException($"MIC master data not found for code '{request.MicCode}' at plant '{request.MicPlantId}'.");

        var mic = new QmInspectionPlanMicEntity
        {
            TenantId = tenantId,
            PlanHeaderId = planId,
            OperationId = request.OperationId,
            CharacteristicNo = request.CharacteristicNo,
            MicCode = request.MicCode,
            MicPlantId = request.MicPlantId,
            MicVersion = 1,
            IsQuantitative = request.IsQuantitative,
            ShortText = request.ShortText,
            InspectionMethodCode = request.InspectionMethodCode,
            SamplingProcedureCode = request.SamplingProcedureCode
        };

        _db.QmInspectionPlanMics.Add(mic);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntryDto
        {
            TenantId = tenantId,
            ModuleName = "QM",
            ActionType = YuktiraERP.Core.Domain.Common.ActionType.Update,
            EntityName = "QmInspectionPlanMic",
            EntityId = mic.Id.ToString(),
            NewValue = System.Text.Json.JsonSerializer.Serialize(new { mic.MicCode, mic.MicPlantId, mic.ShortText })
        });

        return await MapToDtoAsync(planId, tenantId);
    }

    public async Task<InspectionPlanDto?> ResolvePlanAsync(InspectionPlanResolveRequest request, Guid tenantId)
    {
        var specificPlan = await _db.QmInspectionPlanHeaders
            .Where(h => h.TenantId == tenantId
                && h.PlantId == request.PlantId
                && h.MaterialId == request.MaterialId
                && h.Usage == request.Usage
                && h.OverallStatus == "4")
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync();

        if (specificPlan != null)
            return await MapToDtoAsync(specificPlan.Id, tenantId);

        var genericPlan = await _db.QmInspectionPlanHeaders
            .Where(h => h.TenantId == tenantId
                && h.MaterialId == null
                && h.PlantId == request.PlantId
                && h.Usage == request.Usage
                && h.OverallStatus == "4")
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync();

        return genericPlan != null ? await MapToDtoAsync(genericPlan.Id, tenantId) : null;
    }

    public async Task<InspectionPlanDto?> GetPlanAsync(Guid planId, Guid tenantId)
    {
        var header = await _db.QmInspectionPlanHeaders
            .FirstOrDefaultAsync(h => h.Id == planId && h.TenantId == tenantId);

        return header != null ? await MapToDtoAsync(planId, tenantId) : null;
    }

    public async Task<List<InspectionPlanDto>> GetPlansAsync(Guid tenantId, string? plantId = null, string? materialId = null)
    {
        var query = _db.QmInspectionPlanHeaders
            .Where(h => h.TenantId == tenantId);

        if (!string.IsNullOrEmpty(plantId))
            query = query.Where(h => h.PlantId == plantId);
        if (!string.IsNullOrEmpty(materialId))
            query = query.Where(h => h.MaterialId == materialId);

        var headers = await query.OrderByDescending(h => h.CreatedAt).ToListAsync();

        var result = new List<InspectionPlanDto>();
        foreach (var h in headers)
        {
            result.Add(await MapToDtoAsync(h.Id, tenantId));
        }
        return result;
    }

    private async Task<InspectionPlanDto> MapToDtoAsync(Guid planId, Guid tenantId)
    {
        var header = await _db.QmInspectionPlanHeaders.FirstAsync(h => h.Id == planId);
        var operations = await _db.QmInspectionPlanOperations
            .Where(o => o.PlanHeaderId == planId && o.TenantId == tenantId)
            .OrderBy(o => o.OperationNo)
            .ToListAsync();

        var operationIds = operations.Select(o => o.Id).ToList();
        var mics = await _db.QmInspectionPlanMics
            .Where(m => operationIds.Contains(m.OperationId) && m.TenantId == tenantId)
            .OrderBy(m => m.CharacteristicNo)
            .ToListAsync();

        return new InspectionPlanDto
        {
            Id = header.Id,
            TenantId = header.TenantId,
            GroupKey = header.GroupKey,
            GroupCounter = header.GroupCounter,
            PlantId = header.PlantId,
            MaterialId = header.MaterialId,
            Usage = header.Usage,
            OverallStatus = header.OverallStatus,
            LotSizeFrom = header.LotSizeFrom,
            LotSizeTo = header.LotSizeTo,
            Description = header.Description,
            ValidFrom = header.ValidFrom,
            ValidTo = header.ValidTo,
            CreatedAt = header.CreatedAt,
            Operations = operations.Select(o => new InspectionPlanOperationDto
            {
                Id = o.Id,
                OperationNo = o.OperationNo,
                OperationDescription = o.OperationDescription,
                WorkCenter = o.WorkCenter,
                BaseQuantity = o.BaseQuantity,
                Characteristics = mics
                    .Where(m => m.OperationId == o.Id)
                    .Select(m => new InspectionPlanMicDto
                    {
                        Id = m.Id,
                        CharacteristicNo = m.CharacteristicNo,
                        MicCode = m.MicCode,
                        MicPlantId = m.MicPlantId,
                        MicVersion = m.MicVersion,
                        IsQuantitative = m.IsQuantitative,
                        ShortText = m.ShortText,
                        InspectionMethodCode = m.InspectionMethodCode,
                        InspectionMethodVersion = m.InspectionMethodVersion,
                        SamplingProcedureCode = m.SamplingProcedureCode
                    }).ToList()
            }).ToList()
        };
    }
}
