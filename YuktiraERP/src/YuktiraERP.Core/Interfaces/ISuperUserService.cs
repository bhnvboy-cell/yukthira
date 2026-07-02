namespace YuktiraERP.Core.Interfaces;

public class SuperUserPermissionResult
{
    public bool CanOverrideApprovals { get; set; }
    public bool CanUnlockDocuments { get; set; }
    public bool CanResetPasswords { get; set; }
    public bool CanImpersonate { get; set; }
    public bool CanModifyWorkflows { get; set; }
    public bool CanModifyNumberRanges { get; set; }
    public bool CanModifyDashboards { get; set; }
    public bool CanModifyCustomization { get; set; }
    public bool CanAccessAuditLogs { get; set; }
    public bool CanEnableModules { get; set; }
    public bool CanManageTenants { get; set; }
    public bool CanManagePlugins { get; set; }
}

public interface ISuperUserService
{
    Task<bool> IsSuperUserAsync(Guid userId);
    Task<SuperUserPermissionResult> GetPermissionsAsync(Guid userId);
    Task<bool> HasPermissionAsync(Guid userId, string permission);
}
