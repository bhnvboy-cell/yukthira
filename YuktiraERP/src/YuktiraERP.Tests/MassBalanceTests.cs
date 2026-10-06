using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class MassBalanceTests
{
    private static YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new YuktiraDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static MassBalanceCalculator CreateService(YuktiraDbContext db, Guid tenantId, MassBalanceOptions? options = null)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(tenantId);
        return new MassBalanceCalculator(db, tenant.Object, Options.Create(options ?? new MassBalanceOptions()));
    }

    [Fact]
    public void MB01_PureCalculate_PhysicalAndDrySubstanceBalances()
    {
        using var db = CreateDb();
        var service = CreateService(db, Guid.NewGuid());

        var result = service.Calculate(new MassBalanceInput
        {
            Streams = new List<MassBalanceStream>
            {
                new MassBalanceStream { Name = "Corn", Kind = MassBalanceKinds.RawInput, MassKg = 1000m, MoisturePct = 20m },
                new MassBalanceStream { Name = "Starch", Kind = MassBalanceKinds.PrimaryOutput, MassKg = 780m, MoisturePct = 10m },
                new MassBalanceStream { Name = "Bran", Kind = MassBalanceKinds.CoProduct, MassKg = 50m }
            }
        });

        Assert.Equal(1000m, result.TotalInputKg);
        Assert.Equal(780m, result.TotalOutputKg);
        Assert.Equal(50m, result.TotalCoProductKg);
        Assert.Equal(170m, result.YieldLossKg);
        Assert.Equal(17m, result.YieldLossPct);
        Assert.Equal(83m, result.PhysicalYieldPct);
        Assert.Equal(800m, result.DrySubstanceInputKg);
        Assert.Equal(752m, result.DrySubstanceOutputKg);
        Assert.Equal(48m, result.DrySubstanceLossKg);
        Assert.Equal(94m, result.DrySubstanceYieldPct);
        Assert.Equal(2m, result.ThresholdPct);
        Assert.Equal("Flagged", result.Status);
        Assert.Equal(Guid.Empty, result.ProductionOrderId);

        using var breakdown = JsonDocument.Parse(result.BreakdownJson);
        Assert.True(breakdown.RootElement.TryGetProperty("physicalYieldPct", out _));
        Assert.True(breakdown.RootElement.TryGetProperty("kindSubtotals", out _));
    }

    [Fact]
    public void MB01_ThresholdBoundary_StatusSwitchesExactlyAtTolerance()
    {
        using var db = CreateDb();
        var service = CreateService(db, Guid.NewGuid());

        MassBalanceResult MakeResult(decimal outputKg) => service.Calculate(new MassBalanceInput
        {
            Streams = new List<MassBalanceStream>
            {
                new MassBalanceStream { Name = "Feed", Kind = MassBalanceKinds.RawInput, MassKg = 100m },
                new MassBalanceStream { Name = "Product", Kind = MassBalanceKinds.PrimaryOutput, MassKg = outputKg }
            },
            ThresholdPct = 2m
        });

        var atTolerance = MakeResult(98m);
        Assert.Equal(2m, atTolerance.YieldLossPct);
        Assert.Equal("WithinTolerance", atTolerance.Status);

        var beyondTolerance = MakeResult(97.99m);
        Assert.Equal(2.01m, beyondTolerance.YieldLossPct);
        Assert.Equal("Flagged", beyondTolerance.Status);
    }

    [Fact]
    public void MB02_EmptyOrZeroInput_ReturnsZerosWithoutThrowing()
    {
        using var db = CreateDb();
        var service = CreateService(db, Guid.NewGuid());

        var empty = service.Calculate(new MassBalanceInput());
        Assert.Equal(0m, empty.TotalInputKg);
        Assert.Equal(0m, empty.TotalOutputKg);
        Assert.Equal(0m, empty.TotalCoProductKg);
        Assert.Equal(0m, empty.YieldLossKg);
        Assert.Equal(0m, empty.YieldLossPct);
        Assert.Equal(0m, empty.PhysicalYieldPct);
        Assert.Equal(0m, empty.DrySubstanceInputKg);
        Assert.Equal(0m, empty.DrySubstanceOutputKg);
        Assert.Equal(0m, empty.DrySubstanceLossKg);
        Assert.Equal(0m, empty.DrySubstanceYieldPct);
        Assert.Equal("WithinTolerance", empty.Status);
        using (JsonDocument.Parse(empty.BreakdownJson)) { }

        var degenerate = service.Calculate(new MassBalanceInput
        {
            Streams = new List<MassBalanceStream>
            {
                new MassBalanceStream { Name = "Damp", Kind = MassBalanceKinds.RawInput, MassKg = 0m, MoisturePct = 100m }
            }
        });
        Assert.Equal(0m, degenerate.TotalInputKg);
        Assert.Equal(0m, degenerate.DrySubstanceInputKg);
        Assert.Equal(0m, degenerate.PhysicalYieldPct);
        Assert.Equal(0m, degenerate.DrySubstanceYieldPct);
        Assert.Equal("WithinTolerance", degenerate.Status);
    }

    [Fact]
    public async Task MB03_CalculateForOrderAsync_PersistsEntityWithTenantAndBreakdown()
    {
        using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var order = new ProductionOrderEntity
        {
            TenantId = tenantId,
            OrderNumber = "PRO-1001",
            ProductName = "Starch",
            Quantity = 1000m,
            BaseUOM = "kg",
            Status = "COMPLETED",
            YieldQty = 780m,
            ScrapQty = 20m,
            ConfirmedAt = DateTime.UtcNow
        };
        db.ProductionOrders.Add(order);
        db.ProductionOrderItems.Add(new ProductionOrderItemEntity
        {
            ProductionOrderId = order.Id,
            MaterialName = "Corn",
            RequiredQty = 1000m,
            IssuedQty = 1000m,
            UOM = "kg",
            Status = "ISSUED"
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, tenantId);
        var entity = await service.CalculateForOrderAsync(order.Id, "tester", CancellationToken.None);

        Assert.Equal(tenantId, entity.TenantId);
        Assert.Equal(order.Id, entity.ProductionOrderId);
        Assert.Equal("PRO-1001", entity.OrderNumber);
        Assert.Equal(1000m, entity.TotalInputKg);
        Assert.Equal(780m, entity.TotalOutputKg);
        Assert.Equal(220m, entity.YieldLossKg);
        Assert.Equal(22m, entity.YieldLossPct);
        Assert.Equal("Flagged", entity.Status);

        var stored = await db.MassBalanceResults.AsNoTracking()
            .SingleAsync(r => r.ProductionOrderId == order.Id);
        Assert.Equal(tenantId, stored.TenantId);
        Assert.Equal(1000m, stored.TotalInputKg);
        Assert.Equal(780m, stored.TotalOutputKg);
        Assert.Equal(2m, stored.ThresholdPct);

        using (var breakdown = JsonDocument.Parse(stored.BreakdownJson))
        {
            Assert.True(breakdown.RootElement.TryGetProperty("physicalYieldPct", out _));
            Assert.Equal(78m, breakdown.RootElement.GetProperty("physicalYieldPct").GetDecimal());
        }

        IMassBalanceCalculator asInterface = service;
        var dto = await asInterface.CalculateForOrderAsync(order.Id, "tester", CancellationToken.None);
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.NotEqual(entity.Id, dto.Id);
        Assert.Equal(order.Id, dto.ProductionOrderId);
        Assert.Equal(1000m, dto.TotalInputKg);
        Assert.Equal(78m, dto.PhysicalYieldPct);
        Assert.Equal(220m, dto.DrySubstanceLossKg);
        Assert.Equal(2, await db.MassBalanceResults.AsNoTracking().CountAsync(r => r.ProductionOrderId == order.Id));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CalculateForOrderAsync(Guid.NewGuid(), "tester", CancellationToken.None));
    }

    [Fact]
    public void MB04_MoistureAdjustedYield_MatchesDrySubstanceBalance()
    {
        using var db = CreateDb();
        var service = CreateService(db, Guid.NewGuid());

        var wetBasis = 10000m;
        var inputMoisture = 65m;
        var outputMoisture = 12m;
        var drySubstance = wetBasis * (1m - inputMoisture / 100m);
        var adjusted = drySubstance / (1m - outputMoisture / 100m);

        Assert.Equal(3500m, drySubstance);
        Assert.Equal(3977.27m, Math.Round(adjusted, 2));

        var result = service.Calculate(new MassBalanceInput
        {
            Streams = new List<MassBalanceStream>
            {
                new MassBalanceStream { Name = "Wet cake", Kind = MassBalanceKinds.RawInput, MassKg = wetBasis, MoisturePct = inputMoisture },
                new MassBalanceStream { Name = "Dried product", Kind = MassBalanceKinds.PrimaryOutput, MassKg = adjusted, MoisturePct = outputMoisture }
            },
            ThresholdPct = 2m
        });

        Assert.Equal(3977.2727m, result.TotalOutputKg);
        Assert.Equal(39.7727m, result.PhysicalYieldPct);
        Assert.Equal(3500m, result.DrySubstanceInputKg);
        Assert.Equal(3500m, result.DrySubstanceOutputKg);
        Assert.Equal(0m, result.DrySubstanceLossKg);
        Assert.Equal(100m, result.DrySubstanceYieldPct);
        Assert.Equal("Flagged", result.Status);
    }

    [Fact]
    public async Task MB05_FallsBackToBomComponents_WhenNothingWasIssued()
    {
        using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var order = new ProductionOrderEntity
        {
            TenantId = tenantId,
            OrderNumber = "PRO-1002",
            ProductName = "Starch",
            Quantity = 500m,
            Status = "COMPLETED",
            YieldQty = 400m,
            ScrapQty = 10m,
            ConfirmedAt = DateTime.UtcNow
        };
        db.ProductionOrders.Add(order);
        db.BillOfMaterials.Add(new BillOfMaterialEntity
        {
            TenantId = tenantId,
            ProductName = "Starch",
            ComponentName = "Corn",
            Quantity = 2m,
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, tenantId);
        var entity = await service.CalculateForOrderAsync(order.Id, "tester", CancellationToken.None);

        Assert.Equal(1000m, entity.TotalInputKg);
        Assert.Equal(400m, entity.TotalOutputKg);
        Assert.Equal(600m, entity.YieldLossKg);
        Assert.Equal(60m, entity.YieldLossPct);
        Assert.Equal("Flagged", entity.Status);
    }
}
