using YuktiraERP.Core.Domain.Security;

namespace YuktiraERP.Core.Interfaces;

public interface IRbacService
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUserRolesAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<bool> AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedBy = null, Guid? branchId = null, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<bool> RevokeRoleAsync(Guid userId, Guid roleId, Guid? tenantId = null, CancellationToken cancellationToken = default);
}

public interface ISodConflictEngine
{
    /// <summary>
    /// Preventative check evaluated prior to granting a new role to a user.
    /// Checks if the proposed role conflicts with any roles the user already possesses.
    /// </summary>
    Task<SodConflictResult> CheckRoleAssignmentConflictsAsync(
        Guid userId,
        string proposedRoleOrPermission,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transactional / detective check evaluated when executing an action on a document.
    /// Enforces Maker-Checker invariants (e.g. Requester cannot approve, Creator cannot post).
    /// </summary>
    Task<SodConflictResult> CheckActionExecutionConflictsAsync(
        Guid userId,
        string documentType,
        string proposedAction,
        Guid? documentCreatorUserId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns default out-of-the-box toxic combinations (SOD-001 through SOD-006).
    /// </summary>
    IReadOnlyList<SodRuleEntity> GetPredefinedRules();
}
