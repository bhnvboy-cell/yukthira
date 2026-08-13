using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class CostAllocationServiceTests
{
    private static YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task RunAllocation_SplitsProportionally()
    {
        var db = CreateInMemoryDb();
        var svc = new CostAllocationService(db);
        var tenantId = Guid.NewGuid();

        var run = await svc.RunAllocationAsync(tenantId, new CostAllocationRunRequest
        {
            Period = "2026-08",
            TotalAmount = 10000m,
            CostElementCode = "RENT",
            Basis = "Headcount",
            BasisValues =
            {
                new CostAllocationBasisDto { CostCenterCode = "CC-A", CostCenterName = "A", BasisValue = 3m },
                new CostAllocationBasisDto { CostCenterCode = "CC-B", CostCenterName = "B", BasisValue = 1m }
            }
        }, "tester");

        Assert.Equal("Completed", run.Status);
        Assert.Equal(10000m, run.TotalAllocated);

        var details = await svc.GetRunDetailsAsync(tenantId, run.Id);
        Assert.Equal(2, details.Count);
        Assert.Equal(7500m, details.First(d => d.CostCenterCode == "CC-A").Amount);
        Assert.Equal(75m, details.First(d => d.CostCenterCode == "CC-A").SharePercent);
        Assert.Equal(2500m, details.First(d => d.CostCenterCode == "CC-B").Amount);
        Assert.Equal(25m, details.First(d => d.CostCenterCode == "CC-B").SharePercent);
        Assert.Equal(10000m, details.Sum(d => d.Amount));
    }

    [Fact]
    public async Task RunAllocation_ZeroBasis_Throws()
    {
        var db = CreateInMemoryDb();
        var svc = new CostAllocationService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RunAllocationAsync(
            Guid.NewGuid(),
            new CostAllocationRunRequest
            {
                TotalAmount = 100m,
                CostElementCode = "RENT",
                Basis = "Headcount",
                BasisValues = { new CostAllocationBasisDto { CostCenterCode = "CC", BasisValue = 0m } }
            },
            "tester"));
    }

    [Fact]
    public async Task RunAllocation_NegativeAmount_Throws()
    {
        var db = CreateInMemoryDb();
        var svc = new CostAllocationService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RunAllocationAsync(
            Guid.NewGuid(),
            new CostAllocationRunRequest
            {
                TotalAmount = -1m,
                CostElementCode = "RENT",
                Basis = "Headcount",
                BasisValues = { new CostAllocationBasisDto { CostCenterCode = "CC", BasisValue = 1m } }
            },
            "tester"));
    }

    [Fact]
    public async Task Utilization_ComputesAgainstBudget()
    {
        var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.CostCenters.Add(new CostCenterEntity
        {
            Id = Guid.NewGuid(),
            Code = "CC-A",
            Name = "A",
            PlannedBudget = 10000m
        });
        await db.SaveChangesAsync();

        var svc = new CostAllocationService(db);
        var run = await svc.RunAllocationAsync(tenantId, new CostAllocationRunRequest
        {
            TotalAmount = 5000m,
            CostElementCode = "RENT",
            Basis = "Headcount",
            BasisValues = { new CostAllocationBasisDto { CostCenterCode = "CC-A", CostCenterName = "A", BasisValue = 1m } }
        }, "tester");

        var util = await svc.GetUtilizationAsync(tenantId, run.Id);
        var row = util.Single();
        Assert.Equal(50m, row.UtilizationPercent);
        Assert.Equal(5000m, row.Allocated);
        Assert.Equal(10000m, row.PlannedBudget);
    }

    [Fact]
    public async Task CreateRule_DuplicateName_Throws()
    {
        var db = CreateInMemoryDb();
        var svc = new CostAllocationService(db);
        var tenantId = Guid.NewGuid();

        await svc.CreateRuleAsync(tenantId, new CostAllocationRuleDto
        {
            Name = "Rent",
            CostElementCode = "RENT",
            Basis = "Headcount"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateRuleAsync(tenantId, new CostAllocationRuleDto
        {
            Name = "Rent",
            CostElementCode = "UTIL",
            Basis = "FloorArea"
        }));
    }
}