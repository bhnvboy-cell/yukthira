using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class AdminUserService : IAdminUserService
{
    public static readonly string[] AllowedRoles = { "SUPER_USER", "ADMIN", "POWER_USER", "NORMAL_USER", "READ_ONLY" };

    private static readonly PasswordHasher<AdminUserEntity> _hasher = new();

    private readonly YuktiraDbContext _db;

    public AdminUserService(YuktiraDbContext db) => _db = db;

    public async Task<List<AdminUserDto>> GetAllAsync()
    {
        var users = await _db.AdminUsers.OrderBy(u => u.UserName).ToListAsync();
        return users.Select(ToDto).ToList();
    }

    public async Task<AdminUserDto?> GetByIdAsync(Guid id)
    {
        var user = await _db.AdminUsers.FindAsync(id);
        return user == null ? null : ToDto(user);
    }

    public async Task<(bool Success, string? Error, AdminUserDto? User)> CreateAsync(AdminUserCreateRequest request)
    {
        var userName = request.UserId?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(userName))
            return (false, "User ID is required", null);
        if (userName.Length > 50)
            return (false, "User ID must be 50 characters or fewer", null);
        if (!IsAllowedRole(request.Role))
            return (false, $"Role must be one of: {string.Join(", ", AllowedRoles)}", null);

        var minLen = await GetConfigIntAsync("auth.password_min_length", 8);
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < minLen)
            return (false, $"Password must be at least {minLen} characters", null);

        if (await _db.AdminUsers.AnyAsync(u => u.UserName.ToLower() == userName.ToLower()))
            return (false, "A user with this User ID already exists", null);

        var email = request.Email?.Trim() ?? "";
        if (email.Length > 0)
        {
            var emailTaken = await _db.AdminUsers.AnyAsync(u => u.Email.ToLower() == email.ToLower());
            if (emailTaken)
                return (false, "A user with this email already exists", null);
        }

        var entity = new AdminUserEntity
        {
            UserId = userName,
            UserName = userName,
            Email = email,
            Role = request.Role,
            IsActive = true,
            IsSuperUser = request.Role == "SUPER_USER",
            PasswordHash = _hasher.HashPassword(new AdminUserEntity(), request.Password)
        };
        _db.AdminUsers.Add(entity);
        await _db.SaveChangesAsync();
        return (true, null, ToDto(entity));
    }

    public async Task<(bool Success, string? Error, AdminUserDto? User)> UpdateAsync(Guid id, AdminUserUpdateRequest request, string actorUserName)
    {
        var user = await _db.AdminUsers.FindAsync(id);
        if (user == null)
            return (false, "User not found", null);
        if (!IsAllowedRole(request.Role))
            return (false, $"Role must be one of: {string.Join(", ", AllowedRoles)}", null);

        var isSelf = string.Equals(user.UserName, actorUserName, StringComparison.OrdinalIgnoreCase);
        if (isSelf && request.Role != "SUPER_USER")
            return (false, "You cannot remove your own Super User role", null);
        if (isSelf && !request.IsActive)
            return (false, "You cannot deactivate your own account", null);

        if (user.IsSuperUser && (request.Role != "SUPER_USER" || !request.IsActive))
        {
            var otherActiveSuper = await _db.AdminUsers.AnyAsync(u => u.Id != id && u.IsActive && u.IsSuperUser);
            if (!otherActiveSuper)
                return (false, "Cannot demote or deactivate the only active Super User", null);
        }

        user.Email = request.Email?.Trim() ?? "";
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.IsSuperUser = request.Role == "SUPER_USER";
        if (!request.IsActive)
            await RevokeRefreshTokensAsync(user.Id);
        await _db.SaveChangesAsync();
        return (true, null, ToDto(user));
    }

    public async Task<(bool Success, string? Error)> SetActiveAsync(Guid id, bool isActive, string actorUserName)
    {
        var user = await _db.AdminUsers.FindAsync(id);
        if (user == null)
            return (false, "User not found");

        var isSelf = string.Equals(user.UserName, actorUserName, StringComparison.OrdinalIgnoreCase);
        if (isSelf && !isActive)
            return (false, "You cannot deactivate your own account");

        if (!isActive && user.IsSuperUser)
        {
            var otherActiveSuper = await _db.AdminUsers.AnyAsync(u => u.Id != id && u.IsActive && u.IsSuperUser);
            if (!otherActiveSuper)
                return (false, "Cannot deactivate the only active Super User");
        }

        user.IsActive = isActive;
        if (!isActive)
            await RevokeRefreshTokensAsync(user.Id);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(Guid id, string newPassword)
    {
        var user = await _db.AdminUsers.FindAsync(id);
        if (user == null)
            return (false, "User not found");

        var minLen = await GetConfigIntAsync("auth.password_min_length", 8);
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < minLen)
            return (false, $"Password must be at least {minLen} characters");

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await RevokeRefreshTokensAsync(user.Id);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnlockAsync(Guid id)
    {
        var user = await _db.AdminUsers.FindAsync(id);
        if (user == null)
            return (false, "User not found");

        user.LockedUntil = null;
        user.FailedLoginAttempts = 0;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private static bool IsAllowedRole(string? role) =>
        AllowedRoles.Contains(role ?? "");

    private static AdminUserDto ToDto(AdminUserEntity u) => new()
    {
        Id = u.Id,
        UserId = u.UserName,
        UserName = u.UserName,
        Email = u.Email,
        Role = u.Role,
        IsActive = u.IsActive,
        IsSuperUser = u.IsSuperUser,
        IsLocked = u.LockedUntil.HasValue && u.LockedUntil > DateTime.UtcNow,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt
    };

    private async Task RevokeRefreshTokensAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
        foreach (var t in tokens)
            t.IsRevoked = true;
    }

    private async Task<int> GetConfigIntAsync(string key, int defaultValue)
    {
        var entry = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
        return entry != null && int.TryParse(entry.Value, out var val) ? val : defaultValue;
    }
}
