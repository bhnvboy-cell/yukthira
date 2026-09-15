using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IInspectionPlanService
{
    Task<InspectionPlanDto> CreateGenericPlanAsync(InspectionPlanCreateRequest request, Guid tenantId);
    Task<InspectionPlanDto> AssignMicToOperationAsync(Guid planId, InspectionPlanMicAssignRequest request, Guid tenantId);
    Task<InspectionPlanDto?> ResolvePlanAsync(InspectionPlanResolveRequest request, Guid tenantId);
    Task<InspectionPlanDto?> GetPlanAsync(Guid planId, Guid tenantId);
    Task<List<InspectionPlanDto>> GetPlansAsync(Guid tenantId, string? plantId = null, string? materialId = null);
}

public interface IAutoInspectionPlanGenerator
{
    Task<InspectionPlanDto> GeneratePlanAsync(AutoGenerationRequest request, Guid tenantId);
}
