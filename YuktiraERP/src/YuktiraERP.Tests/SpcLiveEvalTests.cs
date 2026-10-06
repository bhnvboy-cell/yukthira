using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class SpcLiveEvalTests
{
    private const string Characteristic = "Thickness";

    private static YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new YuktiraDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static SpcEngineService CreateService(YuktiraDbContext db, Guid tenantId)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(tenantId);
        return new SpcEngineService(db, tenant.Object);
    }

    private static async Task SeedHistoryAsync(YuktiraDbContext db, Guid tenantId, int lotCount = 10, int perLot = 3)
    {
        var means = new[] { 2.00m, 2.02m, 1.98m, 2.01m, 1.99m, 2.02m, 1.98m, 2.00m, 1.99m, 2.01m };
        var offsets = new[] { -0.02m, 0m, 0.02m };
        var baseTime = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var lot = 0; lot < lotCount; lot++)
        {
            var lotNumber = "LOT-LIVE-" + (lot + 1);
            for (var i = 0; i < perLot; i++)
            {
                db.InspectionResults.Add(new InspectionResultEntity
                {
                    TenantId = tenantId,
                    LotNumber = lotNumber,
                    Characteristic = Characteristic,
                    Result = "Pass",
                    Evaluation = "Pass",
                    TargetMin = 1.9m,
                    TargetMax = 2.1m,
                    MeasuredValue = means[lot % means.Length] + offsets[i % offsets.Length],
                    Unit = "mm",
                    Status = "Passed",
                    CreatedAt = baseTime.AddMinutes(lot * 10 + i)
                });
            }
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SP01_LivePoint_InControl_NoViolations()
    {
        using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedHistoryAsync(db, tenantId);

        var service = CreateService(db, tenantId);
        var evaluation = await service.EvaluateLivePointAsync(new SpcLivePointRequest
        {
            Characteristic = Characteristic,
            MeasuredValue = 2.005m
        }, CancellationToken.None);

        Assert.Null(evaluation.Error);
        Assert.Equal(30, evaluation.FoundPoints);
        Assert.Equal(10, evaluation.PointIndex);
        Assert.True(evaluation.InControl);
        Assert.Empty(evaluation.Violations);
        Assert.Equal(2.00, evaluation.Xbar, 4);
        Assert.Equal(2.04092, evaluation.Ucl, 4);
        Assert.Equal(1.95908, evaluation.Lcl, 4);
        Assert.Equal(0.04, evaluation.RBar, 4);
        Assert.Equal(0.0, evaluation.RLcl, 4);
        Assert.NotNull(evaluation.Cp);
        Assert.NotNull(evaluation.Cpk);
        Assert.True(evaluation.Cp!.Value > 1);
    }

    [Fact]
    public async Task SP02_LivePoint_BeyondLimit_ReportsNelsonRule1()
    {
        using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedHistoryAsync(db, tenantId);

        var service = CreateService(db, tenantId);
        var evaluation = await service.EvaluateLivePointAsync(new SpcLivePointRequest
        {
            Characteristic = Characteristic,
            MeasuredValue = 2.20m
        }, CancellationToken.None);

        Assert.Null(evaluation.Error);
        Assert.Equal(30, evaluation.FoundPoints);
        Assert.False(evaluation.InControl);
        Assert.Contains(evaluation.Violations, v => v.Rule == "1");
        Assert.Contains(evaluation.Violations, v => !string.IsNullOrWhiteSpace(v.Description));
        Assert.True(evaluation.Xbar + 0.04092 < 2.20);
    }

    [Fact]
    public async Task SP03_UnknownOrShortHistory_ReturnsStructuredError()
    {
        using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var service = CreateService(db, tenantId);

        var withoutCharacteristic = await service.EvaluateLivePointAsync(new SpcLivePointRequest
        {
            MeasuredValue = 2.0m
        }, CancellationToken.None);
        Assert.Equal(0, withoutCharacteristic.FoundPoints);
        Assert.NotNull(withoutCharacteristic.Error);
        Assert.Contains("0 points", withoutCharacteristic.Error!);

        var unknown = await service.EvaluateLivePointAsync(new SpcLivePointRequest
        {
            Characteristic = Characteristic,
            MeasuredValue = 2.0m
        }, CancellationToken.None);
        Assert.Equal(0, unknown.FoundPoints);
        Assert.NotNull(unknown.Error);
        Assert.Contains("0 points", unknown.Error!);
        Assert.False(unknown.InControl);
        Assert.Empty(unknown.Violations);

        await SeedHistoryAsync(db, tenantId, lotCount: 1, perLot: 3);

        var shortHistory = await service.EvaluateLivePointAsync(new SpcLivePointRequest
        {
            Characteristic = Characteristic,
            MeasuredValue = 2.0m
        }, CancellationToken.None);
        Assert.Equal(3, shortHistory.FoundPoints);
        Assert.NotNull(shortHistory.Error);
        Assert.Contains("3 points", shortHistory.Error!);
        Assert.False(shortHistory.InControl);
        Assert.Empty(shortHistory.Violations);
        Assert.Null(shortHistory.Cp);
    }

    [Fact]
    public async Task SP04_SubgroupSizeOverride_WidensLimitsAndKeepsPointInControl()
    {
        using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedHistoryAsync(db, tenantId);

        var service = CreateService(db, tenantId);
        var evaluation = await service.EvaluateLivePointAsync(new SpcLivePointRequest
        {
            Characteristic = Characteristic,
            MeasuredValue = 2.01m,
            SubgroupSize = 5
        }, CancellationToken.None);

        Assert.Null(evaluation.Error);
        Assert.Equal(30, evaluation.FoundPoints);
        Assert.Equal(2.02308, evaluation.Ucl, 4);
        Assert.Equal(1.97692, evaluation.Lcl, 4);
        Assert.True(evaluation.InControl);
        Assert.Empty(evaluation.Violations);
    }

    [Fact]
    public async Task SP05_LivePointOnAnotherCharacteristic_IsNotInfluencedByHistory()
    {
        using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        await SeedHistoryAsync(db, tenantId);

        var service = CreateService(db, tenantId);
        var evaluation = await service.EvaluateLivePointAsync(new SpcLivePointRequest
        {
            Characteristic = "Colour",
            MeasuredValue = 2.0m
        }, CancellationToken.None);

        Assert.Equal("Colour", evaluation.Characteristic);
        Assert.Equal(0, evaluation.FoundPoints);
        Assert.NotNull(evaluation.Error);
        Assert.Contains("Colour", evaluation.Error!);
        Assert.Equal(0.0, evaluation.Ucl);
        Assert.Equal(0.0, evaluation.RBar);
    }
}
