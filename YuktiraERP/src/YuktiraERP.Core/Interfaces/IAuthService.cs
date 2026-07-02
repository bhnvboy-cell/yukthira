using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Interfaces;

public class LoginRequest
{
    public string ClientNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Language { get; set; } = "EN";
    public string System { get; set; } = "DEV";
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserProfile UserProfile { get; set; } = new();
}

public class UserProfile
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Language { get; set; } = "EN";
    public Guid? TenantId { get; set; }
    public bool IsSuperUser { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string ipAddress, string deviceInfo);
    Task LogoutAsync(Guid userId);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken);
    Task<UserProfile> GetUserProfileAsync(Guid userId);
    Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid? tenantId);
    Task<bool> ValidateTokenAsync(string token);
}
