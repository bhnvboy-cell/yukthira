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

        if (user.MfaEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.MfaCode) ||
                !MfaTotpService.VerifyCode(user.MfaSecret, request.MfaCode, DateTime.UtcNow))
                throw new UnauthorizedAccessException("Two-factor authentication code is invalid or expired");
        }

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t =>
            t.Code == request.ClientNumber && t.Status == "ACTIVE");
        if (tenant == null)
            throw new UnauthorizedAccessException("Invalid client number");

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

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
            Token = HashToken(refreshToken),
            TenantId = tenant?.Id,
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

    public async Task<LoginResponse> ImpersonateAsync(Guid actorUserId, Guid targetUserId)
    {
        var actor = await _db.AdminUsers.FindAsync(actorUserId);
        if (actor == null || !actor.IsSuperUser)
            throw new UnauthorizedAccessException("Impersonation requires superuser rights");

        var target = await _db.AdminUsers.FindAsync(targetUserId);
        if (target == null || !target.IsActive)
            throw new UnauthorizedAccessException("Target user not found or inactive");

        var permissions = await ResolvePermissionsAsync(target.Id, target.Role, null);
        var userProfile = new UserProfile
        {
            UserId = target.Id,
            Username = target.UserName,
            FullName = target.UserName,
            Email = target.Email,
            Role = target.Role,
            Language = "EN",
            TenantId = null,
            IsSuperUser = target.IsSuperUser,
            Permissions = permissions
        };

        var accessToken = GenerateJwtToken(userProfile);
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        _db.AuditLogs.Add(new AuditLogEntity
        {
            Id = Guid.NewGuid(),
            TenantId = null,
            UserId = actorUserId,
            UserName = actor.UserName,
            ModuleName = "Security",
            EntityName = "AdminUser",
            ActionType = "Impersonate",            Description = $"Superuser {actor.UserName} impersonated {target.UserName}",
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = "",
            ExpiresAt = expiresAt,
            UserProfile = userProfile
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == HashToken(refreshToken) && !t.IsRevoked);
        if (stored == null || stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        var user = await _db.AdminUsers.FindAsync(stored.UserId);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive");

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            throw new UnauthorizedAccessException("Account is locked");

        var tenant = stored.TenantId.HasValue ? await _db.Tenants.FindAsync(stored.TenantId.Value) : null;
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
        stored.ReplacedByToken = HashToken(newRefreshToken);

        _db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = HashToken(newRefreshToken),
            TenantId = stored.TenantId,
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

    public async Task<UserProfile> GetUserProfileAsync(Guid userId, Guid? tenantId)
    {
        var user = await _db.AdminUsers.FindAsync(userId);
        if (user == null) throw new UnauthorizedAccessException("User not found");
        var permissions = await ResolvePermissionsAsync(user.Id, user.Role, tenantId);
        return new UserProfile
        {
            UserId = user.Id,
            Username = user.UserName,
            FullName = user.UserName,
            Email = user.Email,
            Role = user.Role,
            Language = "EN",
            TenantId = tenantId,
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
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(GetJwtKey()),
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

    public async Task<MfaSetupResult> SetupMfaAsync(Guid userId)
    {
        var user = await _db.AdminUsers.FindAsync(userId);
        if (user == null) throw new UnauthorizedAccessException("User not found");

        var secret = MfaTotpService.GenerateSecret();
        user.MfaSecret = secret;
        user.MfaEnabled = false;
        await _db.SaveChangesAsync();

        var account = Uri.EscapeDataString($"{user.UserName}@{_configuration["Jwt:Issuer"] ?? "YuktiraERP"}");
        var otpUri = $"otpauth://totp/{account}?secret={secret}&issuer={_configuration["Jwt:Issuer"] ?? "YuktiraERP"}";

        return new MfaSetupResult { Secret = secret, OtpAuthUri = otpUri, Enabled = false };
    }

    public async Task<bool> VerifyAndEnableMfaAsync(Guid userId, string code)
    {
        var user = await _db.AdminUsers.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.MfaSecret)) return false;
        if (!MfaTotpService.VerifyCode(user.MfaSecret, code, DateTime.UtcNow)) return false;

        user.MfaEnabled = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task DisableMfaAsync(Guid userId, string code)
    {
        var user = await _db.AdminUsers.FindAsync(userId);
        if (user == null || !user.MfaEnabled) throw new UnauthorizedAccessException("MFA is not enabled");
        if (!MfaTotpService.VerifyCode(user.MfaSecret, code, DateTime.UtcNow))
            throw new UnauthorizedAccessException("Two-factor authentication code is invalid or expired");

        user.MfaEnabled = false;
        user.MfaSecret = "";
        await _db.SaveChangesAsync();
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
        var key = new SymmetricSecurityKey(GetJwtKey());
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

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private byte[] GetJwtKey()
    {
        var secret = _configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException("JWT signing secret (Jwt:Secret) is not configured or too short.");
        return Encoding.UTF8.GetBytes(secret);
    }
}
