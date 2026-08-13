namespace YuktiraERP.Core.Interfaces;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsSuperUser { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserCreateRequest
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "READ_ONLY";
    public string Password { get; set; } = "";
}

public class AdminUserUpdateRequest
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "READ_ONLY";
    public bool IsActive { get; set; } = true;
}

public interface IAdminUserService
{
    Task<List<AdminUserDto>> GetAllAsync();
    Task<AdminUserDto?> GetByIdAsync(Guid id);
    Task<(bool Success, string? Error, AdminUserDto? User)> CreateAsync(AdminUserCreateRequest request);
    Task<(bool Success, string? Error, AdminUserDto? User)> UpdateAsync(Guid id, AdminUserUpdateRequest request, string actorUserName);
    Task<(bool Success, string? Error)> SetActiveAsync(Guid id, bool isActive, string actorUserName);
    Task<(bool Success, string? Error)> ResetPasswordAsync(Guid id, string newPassword);
    Task<(bool Success, string? Error)> UnlockAsync(Guid id);
}
