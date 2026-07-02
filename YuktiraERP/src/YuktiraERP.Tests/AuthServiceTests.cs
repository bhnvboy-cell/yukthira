using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class AuthServiceTests
{
    private YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "TestSecretKeyThatIsAtLeast32CharactersLong!!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenExpirationHours"] = "8",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();
    }

    private static string HashPassword(string password)
    {
        var hasher = new PasswordHasher<AdminUserEntity>();
        return hasher.HashPassword(new AdminUserEntity(), password);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        var db = CreateInMemoryDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Code = "DEMO", Name = "Demo Tenant" };
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            PasswordHash = HashPassword("Admin@123"),
            Email = "admin@yuktira.com",
            Role = "SuperAdmin",
            IsActive = true
        };
        db.Tenants.Add(tenant);
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync();

        var service = new AuthService(db, CreateConfig());
        var result = await service.LoginAsync(new LoginRequest
        {
            UserId = "admin",
            Password = "Admin@123",
            ClientNumber = "DEMO"
        }, "127.0.0.1", "test-device");

        Assert.NotNull(result);
        Assert.Equal("admin", result.UserProfile?.Username);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_Throws()
    {
        var db = CreateInMemoryDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Code = "DEMO", Name = "Demo Tenant" };
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            PasswordHash = HashPassword("Admin@123"),
            Email = "admin@yuktira.com",
            Role = "SuperAdmin",
            IsActive = true
        };
        db.Tenants.Add(tenant);
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync();

        var service = new AuthService(db, CreateConfig());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(new LoginRequest
        {
            UserId = "admin",
            Password = "WrongPassword!",
            ClientNumber = "DEMO"
        }, "127.0.0.1", "test-device"));
    }

    [Fact]
    public async Task LoginAsync_LockedUser_Throws()
    {
        var db = CreateInMemoryDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Code = "DEMO", Name = "Demo Tenant" };
        var user = new AdminUserEntity
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            PasswordHash = HashPassword("Admin@123"),
            Email = "admin@yuktira.com",
            Role = "SuperAdmin",
            IsActive = true,
            FailedLoginAttempts = 5,
            LockedUntil = DateTime.UtcNow.AddMinutes(30)
        };
        db.Tenants.Add(tenant);
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync();

        var service = new AuthService(db, CreateConfig());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(new LoginRequest
        {
            UserId = "admin",
            Password = "Admin@123",
            ClientNumber = "DEMO"
        }, "127.0.0.1", "test-device"));
    }
}
