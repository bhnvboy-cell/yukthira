using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class ReleaseStrategyServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private async Task SeedStrategiesAsync(YuktiraDbContext db)
    {
        var tenantId = Guid.NewGuid();

        db.ReleaseStrategies.Add(new ReleaseStrategyEntity
        {
            TenantId = tenantId,
            Code = "RS01",
            Name = "Standard PR",
            DocumentType = "PR",
            MinAmount = 0,
            MaxAmount = 10000,
            IsActive = true
        });
        db.ReleaseStrategies.Add(new ReleaseStrategyEntity
        {
            TenantId = tenantId,
            Code = "RS02",
            Name = "High Value PR",
            DocumentType = "PR",
            MinAmount = 10000,
            MaxAmount = 999999,
            IsActive = true
        });
        db.ReleaseStrategies.Add(new ReleaseStrategyEntity
        {
            TenantId = tenantId,
            Code = "RS03",
            Name = "Standard PO",
            DocumentType = "PO",
            MinAmount = 0,
            MaxAmount = 50000,
            IsActive = true
        });

        await db.SaveChangesAsync();

        var rs01 = await db.ReleaseStrategies.FirstAsync(s => s.Code == "RS01");
        db.ReleaseCodes.Add(new ReleaseCodeEntity
        {
            TenantId = tenantId,
            ReleaseStrategyId = rs01.Id,
            Level = 1,
            Code = "RC01",
            ApproverRole = "PURCHASER",
            IsRequired = true
        });
        db.ReleaseCodes.Add(new ReleaseCodeEntity
        {
            TenantId = tenantId,
            ReleaseStrategyId = rs01.Id,
            Level = 2,
            Code = "RC02",
            ApproverRole = "MANAGER",
            IsRequired = true
        });

        db.ReleaseCodes.Add(new ReleaseCodeEntity
        {
            TenantId = tenantId,
            ReleaseStrategyId = rs01.Id,
            Level = 3,
            Code = "RC03",
            ApproverRole = "ADMIN",
            ApproverUserId = "ADMIN",
            IsRequired = true
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task FindMatchingStrategyAsync_FindsCorrectStrategy()
    {
        var db = CreateDb();
        await SeedStrategiesAsync(db);
        var service = new ReleaseStrategyService(db);

        var strategy = await service.FindMatchingStrategyAsync("PR", 5000m, "", "");

        Assert.NotNull(strategy);
        Assert.Equal("RS01", strategy!.Code);
    }

    [Fact]
    public async Task FindMatchingStrategyAsync_AmountOutOfRange_ReturnsNull()
    {
        var db = CreateDb();
        await SeedStrategiesAsync(db);
        var service = new ReleaseStrategyService(db);

        var strategy = await service.FindMatchingStrategyAsync("PR", 100m, "", "");

        Assert.NotNull(strategy);
        Assert.Equal("RS01", strategy!.Code);
    }

    [Fact]
    public async Task FindMatchingStrategyAsync_NoMatch_ReturnsNull()
    {
        var db = CreateDb();
        await SeedStrategiesAsync(db);
        var service = new ReleaseStrategyService(db);

        var strategy = await service.FindMatchingStrategyAsync("PO", 100000m, "", "");

        Assert.Null(strategy);
    }

    [Fact]
    public async Task GetReleaseCodesAsync_ReturnsCodesOrderedByLevel()
    {
        var db = CreateDb();
        await SeedStrategiesAsync(db);
        var service = new ReleaseStrategyService(db);

        var rs01 = await db.ReleaseStrategies.FirstAsync(s => s.Code == "RS01");
        var codes = await service.GetReleaseCodesAsync(rs01.Id);

        Assert.Equal(3, codes.Count);
        Assert.Equal(1, codes[0].Level);
        Assert.Equal(2, codes[1].Level);
        Assert.Equal(3, codes[2].Level);
    }

    [Fact]
    public async Task ExecuteReleaseStrategyAsync_AdminUser_ReturnsTrue()
    {
        var db = CreateDb();
        await SeedStrategiesAsync(db);
        var service = new ReleaseStrategyService(db);

        var result = await service.ExecuteReleaseStrategyAsync(Guid.NewGuid(), "PR", "ADMIN");

        Assert.True(result);
    }

    [Fact]
    public async Task ExecuteReleaseStrategyAsync_NoRequiredCodes_ReturnsTrue()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.ReleaseCodes.Add(new ReleaseCodeEntity
        {
            TenantId = tenantId,
            ReleaseStrategyId = Guid.NewGuid(),
            Level = 1,
            Code = "RC-NR",
            ApproverRole = "USER",
            IsRequired = false
        });
        await db.SaveChangesAsync();

        var service = new ReleaseStrategyService(db);
        var result = await service.ExecuteReleaseStrategyAsync(Guid.NewGuid(), "PR", "user1");

        Assert.True(result);
    }
}
