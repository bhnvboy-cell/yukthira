using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Domain.Security;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;

namespace YuktiraERP.Infrastructure.Services;

public class RbacService : IRbacService
{
    private readonly YuktiraDbContext _dbContext;
    private readonly ILogger<RbacService>? _logger;

    public RbacService(YuktiraDbContext dbContext, ILogger<RbacService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        string permissionCode,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionCode)) return false;

        var permissions = await GetUserPermissionsAsync(userId, tenantId, cancellationToken);
        return permissions.Contains("*") || permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Join user roles -> role permissions -> permissions
            var roleIds = await _dbContext.Set<IdentityUserRole>()
                .Where(ur => ur.UserId == userId && (tenantId == null || ur.TenantId == tenantId))
                .Select(ur => ur.RoleId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (roleIds.Count == 0) return Array.Empty<string>();

            var permissionIds = await _dbContext.Set<IdentityRolePermission>()
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.PermissionId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var codes = await _dbContext.Set<IdentityPermission>()
                .Where(p => permissionIds.Contains(p.Id))
                .Select(p => p.Code)
                .ToListAsync(cancellationToken);

            return codes.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed querying permissions for user {UserId}", userId);
            return Array.Empty<string>();
        }
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(
        Guid userId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var roleIds = await _dbContext.Set<IdentityUserRole>()
                .Where(ur => ur.UserId == userId && (tenantId == null || ur.TenantId == tenantId))
                .Select(ur => ur.RoleId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var roles = await _dbContext.Set<IdentityRole>()
                .Where(r => roleIds.Contains(r.Id) && r.IsActive)
                .Select(r => r.Code)
                .ToListAsync(cancellationToken);

            return roles.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed querying roles for user {UserId}", userId);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> AssignRoleAsync(
        Guid userId,
        Guid roleId,
        Guid? assignedBy = null,
        Guid? branchId = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _dbContext.Set<IdentityUserRole>()
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && (tenantId == null || ur.TenantId == tenantId), cancellationToken);

            if (existing != null)
            {
                return true; // Already assigned
            }

            var assignment = new IdentityUserRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                RoleId = roleId,
                BranchId = branchId,
                AssignedByUserId = assignedBy,
                AssignedAtUtc = DateTime.UtcNow
            };

            await _dbContext.Set<IdentityUserRole>().AddAsync(assignment, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation("Assigned role {RoleId} to user {UserId}", roleId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
            return false;
        }
    }

    public async Task<bool> RevokeRoleAsync(
        Guid userId,
        Guid roleId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var assignment = await _dbContext.Set<IdentityUserRole>()
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && (tenantId == null || ur.TenantId == tenantId), cancellationToken);

            if (assignment == null) return false;

            _dbContext.Set<IdentityUserRole>().Remove(assignment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger?.LogInformation("Revoked role {RoleId} from user {UserId}", roleId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error revoking role {RoleId} from user {UserId}", roleId, userId);
            return false;
        }
    }
}
