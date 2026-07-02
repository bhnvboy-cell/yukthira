using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly YuktiraDbContext _db;
    private readonly IConfiguration _configuration;
    private static readonly PasswordHasher<AdminUserEntity> _passwordHasher = new();

    public AuthService(YuktiraDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string ipAddress, string deviceInfo)
    {
        var user = await _db.AdminUsers.FirstOrDefaultAsync(u =>
            u.UserName == request.UserId && u.IsActive);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid credentials");

        var maxAttempts = await GetConfigIntAsync("auth.max_login_attempts", 5);
        var lockoutMinutes = await GetConfigIntAsync("auth.lockout_minutes", 15);

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            throw new UnauthorizedAccessException($"Account locked until {user.LockedUntil:u}. Try again later.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= maxAttempts)
                user.LockedUntil = DateTime.UtcNow.AddMinutes(lockoutMinutes);
            await _db.SaveChangesAsync();
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t =>
            t.Code == request.ClientNumber && t.Status == "ACTIVE");

        var permissions = await ResolvePermissionsAsync(user.Id, user.Role, tenant?.Id);
        var userProfile = new UserProfile
        {
            UserId = user.Id,
            Username = user.UserName,
            FullName = user.UserName,
            Email = user.Email,
            Role = user.Role,
            Language = request.Language,
            TenantId = tenant?.Id ?? Guid.Empty,
            IsSuperUser = user.IsSuperUser,
            Permissions = permissions
        };

        var accessToken = GenerateJwtToken(userProfile);
        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(8);

        _db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = expiresAt.AddDays(7),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress
        });
        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserProfile = userProfile
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
        foreach (var t in tokens) t.IsRevoked = true;
        await _db.SaveChangesAsync();
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked);
        if (stored == null || stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        var user = await _db.AdminUsers.FindAsync(stored.UserId);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive");

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            throw new UnauthorizedAccessException("Account is locked");

        var tenant = await _db.Tenants.FirstOrDefaultAsync();
        var permissions = await ResolvePermissionsAsync(user.Id, user.Role, tenant?.Id);
        var userProfile = new UserProfile
        {
            UserId = user.Id,
            Username = user.UserName,
            FullName = user.UserName,
            Email = user.Email,
            Role = user.Role,
            Language = "EN",
            TenantId = tenant?.Id,
            IsSuperUser = user.IsSuperUser,
            Permissions = permissions
        };

        var newAccessToken = GenerateJwtToken(userProfile);
        var newRefreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(8);

        stored.IsRevoked = true;
        stored.ReplacedByToken = newRefreshToken;

        _db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = expiresAt.AddDays(7),
            DeviceInfo = stored.DeviceInfo,
            IpAddress = stored.IpAddress
        });
        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            UserProfile = userProfile
        };
    }

    public async Task<UserProfile> GetUserProfileAsync(Guid userId)
    {
        var user = await _db.AdminUsers.FindAsync(userId);
        if (user == null) throw new UnauthorizedAccessException("User not found");
        var tenant = await _db.Tenants.FirstOrDefaultAsync();
        var permissions = await ResolvePermissionsAsync(user.Id, user.Role, tenant?.Id);
        return new UserProfile
        {
            UserId = user.Id,
            Username = user.UserName,
            FullName = user.UserName,
            Email = user.Email,
            Role = user.Role,
            Language = "EN",
            TenantId = tenant?.Id,
            IsSuperUser = user.IsSuperUser,
            Permissions = permissions
        };
    }

    public async Task<List<string>> GetUserPermissionsAsync(Guid userId, Guid? tenantId)
    {
        var user = await _db.AdminUsers.FindAsync(userId);
        if (user == null) return new();
        return await ResolvePermissionsAsync(userId, user.Role, tenantId);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? "YuktiraERPSuperSecretKey2024!@#$%^&*()Minimum32Chars");
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "YuktiraERP",
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"] ?? "YuktiraERPUsers",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private async Task<List<string>> ResolvePermissionsAsync(Guid userId, string role, Guid? tenantId)
    {
        var perms = new List<string>();
        if (string.IsNullOrEmpty(role)) return perms;

        var roleUpper = role.ToUpperInvariant();
        if (roleUpper is "SUPER_USER" or "ADMIN")
        {
            perms.Add("*");
            return perms;
        }

        var txnPerms = await _db.TransactionPermissions
            .Where(tp => tp.PrincipalType == "Role" && tp.PrincipalValue == role && tp.CanAccess)
            .Select(tp => tp.TransactionCodeId.ToString())
            .ToListAsync();
        perms.AddRange(txnPerms.Select(p => $"TXN:{p}"));

        var userPerms = await _db.TransactionPermissions
            .Where(tp => tp.PrincipalType == "User" && tp.PrincipalValue == userId.ToString() && tp.CanAccess)
            .Select(tp => tp.TransactionCodeId.ToString())
            .ToListAsync();
        perms.AddRange(userPerms.Select(p => $"TXN:{p}"));

        if (!perms.Any()) perms.AddRange(new[] { "READ", "WRITE" });
        return perms;
    }

    private async Task<int> GetConfigIntAsync(string key, int defaultValue)
    {
        var entry = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
        return entry != null && int.TryParse(entry.Value, out var val) ? val : defaultValue;
    }

    private string GenerateJwtToken(UserProfile profile)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Secret"] ?? "YuktiraERPSuperSecretKey2024!@#$%^&*()Minimum32Chars"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profile.UserId.ToString()),
            new(ClaimTypes.Name, profile.Username),
            new(ClaimTypes.Role, profile.Role),
            new("TenantId", profile.TenantId?.ToString() ?? ""),
            new("IsSuperUser", profile.IsSuperUser.ToString().ToLower())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "YuktiraERP",
            audience: _configuration["Jwt:Audience"] ?? "YuktiraERPUsers",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
