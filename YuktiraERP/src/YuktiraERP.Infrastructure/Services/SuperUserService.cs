using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class SuperUserService : ISuperUserService
{
    private readonly YuktiraDbContext _db;

    public SuperUserService(YuktiraDbContext db) => _db = db;

    public async Task<bool> IsSuperUserAsync(Guid userId)
    {
        var user = await _db.AdminUsers.FindAsync(userId);
        return user?.IsSuperUser == true;
    }

    public async Task<SuperUserPermissionResult> GetPermissionsAsync(Guid userId)
    {
        var user = await _db.AdminUsers.FindAsync(userId);
        if (user?.IsSuperUser != true) return new SuperUserPermissionResult();

        return new SuperUserPermissionResult
        {
            CanOverrideApprovals = true,
            CanUnlockDocuments = true,
            CanResetPasswords = true,
            CanImpersonate = true,
            CanModifyWorkflows = true,
            CanModifyNumberRanges = true,
            CanModifyDashboards = true,
            CanModifyCustomization = true,
            CanAccessAuditLogs = true,
            CanEnableModules = true,
            CanManageTenants = true,
            CanManagePlugins = true
        };
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var perms = await GetPermissionsAsync(userId);
        return permission switch
        {
            "OverrideApprovals" => perms.CanOverrideApprovals,
            "UnlockDocuments" => perms.CanUnlockDocuments,
            "ResetPasswords" => perms.CanResetPasswords,
            "Impersonate" => perms.CanImpersonate,
            "ModifyWorkflows" => perms.CanModifyWorkflows,
            "ModifyNumberRanges" => perms.CanModifyNumberRanges,
            "ModifyDashboards" => perms.CanModifyDashboards,
            "ModifyCustomization" => perms.CanModifyCustomization,
            "AccessAuditLogs" => perms.CanAccessAuditLogs,
            "EnableModules" => perms.CanEnableModules,
            "ManageTenants" => perms.CanManageTenants,
            "ManagePlugins" => perms.CanManagePlugins,
            _ => false
        };
    }
}
