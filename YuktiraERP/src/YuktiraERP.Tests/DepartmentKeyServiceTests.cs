using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class DepartmentKeyServiceTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_SavesDepartmentKey()
    {
        var db = CreateDb();
        var service = new DepartmentKeyService(db);
        var tenantId = Guid.NewGuid();
        var entity = new DepartmentKeyEntity
        {
            TenantId = tenantId,
            Code = "PUR",
            Name = "Procurement",
            Description = "Procurement Department",
            CostCenterDefault = "CC-PUR"
        };

        var result = await service.CreateAsync(entity);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("PUR", result.Code);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyActiveForTenant()
    {
        var db = CreateDb();
        var service = new DepartmentKeyService(db);
        var tenantId = Guid.NewGuid();

        db.DepartmentKeys.AddRange(
            new DepartmentKeyEntity { TenantId = tenantId, Code = "PUR", Name = "Procurement", IsActive = true },
            new DepartmentKeyEntity { TenantId = tenantId, Code = "MFG", Name = "Manufacturing", IsActive = false },
            new DepartmentKeyEntity { TenantId = Guid.NewGuid(), Code = "ADM", Name = "Admin", IsActive = true }
        );
        await db.SaveChangesAsync();

        var result = await service.GetAllAsync(tenantId);

        Assert.Single(result);
        Assert.Equal("PUR", result[0].Code);
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsCorrectEntity()
    {
        var db = CreateDb();
        var service = new DepartmentKeyService(db);
        var tenantId = Guid.NewGuid();

        db.DepartmentKeys.Add(new DepartmentKeyEntity
        {
            TenantId = tenantId,
            Code = "WH",
            Name = "Warehouse"
        });
        await db.SaveChangesAsync();

        var result = await service.GetByCodeAsync("WH", tenantId);

        Assert.NotNull(result);
        Assert.Equal("Warehouse", result!.Name);
    }

    [Fact]
    public async Task GetByCodeAsync_NotFound_ReturnsNull()
    {
        var db = CreateDb();
        var service = new DepartmentKeyService(db);

        var result = await service.GetByCodeAsync("INVALID", Guid.NewGuid());

        Assert.Null(result);
    }
}
