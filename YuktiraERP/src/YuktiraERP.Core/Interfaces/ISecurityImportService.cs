using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface ISecurityImportService
{
    Task<SecurityImportResultDto> ImportMasterRolesAsync(List<SecurityImportRowDto> rows, Guid tenantId, Guid userId);
    Task<SecurityImportResultDto> ImportCompositeRolesAsync(List<CompositeRoleImportRowDto> rows, Guid tenantId, Guid userId);
    Task<SecurityImportResultDto> ImportFullRoleMatrixAsync(RoleMatrixImportRequest request, Guid tenantId, Guid userId);
    Task<List<MasterRoleDto>> GetMasterRolesAsync(Guid tenantId, string? module = null);
    Task<List<CompositeRoleDto>> GetCompositeRolesAsync(Guid tenantId, string? module = null);
    Task<List<DerivedRoleDto>> GetDerivedRolesAsync(string compositeRoleId);
    Task<List<RoleHierarchyDto>> GetRoleHierarchyAsync(Guid tenantId);
    Task<UserRoleAssignResultDto> AssignCompositeRoleToUserAsync(UserRoleAssignRequest request, Guid tenantId, string assignedByUserId);
    Task<List<UserRoleAssignmentDto>> GetUserRoleAssignmentsAsync(string userId, Guid tenantId);
    Task<List<RoleTCodeAssignmentDto>> GetRoleTCodePermissionsAsync(string roleId, string roleType, Guid tenantId);
    Task<int> GetTCodeCountAsync(Guid tenantId);
    Task<int> GetRoleCountAsync(Guid tenantId);
}
