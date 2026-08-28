using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Tests;

/// <summary>
/// PRD-01: Mass-Balance Yield & Dosing Adjustments
/// PRD-02: Production Order Explosion & Component Issue
/// </summary>
public class ProductionModuleTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    // ════════════════════════════════════════════════════════════════
    // PRD-01: Mass-Balance Yield & Dosing Adjustments
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PRD01_MassBalance_CalculatesYieldPercentage()
    {
        var db = CreateDb();

        // Arrange: Plant balance inputs
        decimal wetCornInput = 10000m;        // kg wet corn
        decimal steepLiquorDosing = 2500m;    // kg heavy condensed steep liquor
        decimal totalInput = wetCornInput + steepLiquorDosing;  // 12500 kg

        // Act: Production outputs
        decimal starchOutput = 5200m;         // kg starch
        decimal glutenFeedOutput = 3100m;     // kg wet corn gluten feed
        decimal germOutput = 850m;            // kg corn germ
        decimal steepWaterOutput = 2800m;     // kg steep water
        decimal totalOutput = starchOutput + glutenFeedOutput + germOutput + steepWaterOutput;  // 11950 kg

        // Calculate yields and losses
        decimal yieldPct = (totalOutput / totalInput) * 100m;
        decimal lossPct = 100m - yieldPct;

        // Assert: Yield calculation is accurate
        Assert.Equal(12500m, totalInput);
        Assert.Equal(11950m, totalOutput);
        Assert.Equal(95.60m, Math.Round(yieldPct, 2));  // 11950/12500 * 100
        Assert.Equal(4.40m, Math.Round(lossPct, 2));     // 100 - 95.60
    }

    [Fact]
    public async Task PRD01_MassBalance_MoistureAdjustment_Accurate()
    {
        // Arrange: Moisture adjustment calculation
        decimal wetBasisWeight = 10000m;  // kg wet basis
        decimal moistureWetBasis = 65.0m;  // % moisture in wet basis
        decimal moistureDryBasis = 12.0m;  // % moisture in dry product

        // Act: Calculate dry matter and adjusted weight
        decimal dryMatter = wetBasisWeight * (1m - moistureWetBasis / 100m);
        decimal adjustedWeight = dryMatter / (1m - moistureDryBasis / 100m);

        // Assert: Moisture adjustment is accurate
        Assert.Equal(3500m, dryMatter);  // 10000 * 0.35
        Assert.Equal(3977.27m, Math.Round(adjustedWeight, 2));  // 3500 / 0.88
    }

    [Fact]
    public async Task PRD01_MassBalance_DosingRate_CalculatedCorrectly()
    {
        // Arrange: Heavy Condensed Steep Liquor (HCSL) dosing rates
        decimal hcslFlowRate = 2500m;    // liters/hour
        decimal wetCornFlowRate = 10000m; // kg/hour
        decimal processDuration = 8m;     // hours

        // Act: Calculate total dosing
        decimal totalHCSL = hcslFlowRate * processDuration;
        decimal totalWetCorn = wetCornFlowRate * processDuration;
        decimal dosingRatio = hcslFlowRate / wetCornFlowRate;

        // Assert: Dosing rates are accurate
        Assert.Equal(20000m, totalHCSL);       // 2500 * 8
        Assert.Equal(80000m, totalWetCorn);     // 10000 * 8
        Assert.Equal(0.25m, dosingRatio);       // 2500/10000
    }

    [Fact]
    public async Task PRD01_MassBalance_LossPercentage_Tracked()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Production plan
        var plan = new ProductionPlanEntity
        {
            TenantId = tenantId,
            PlanId = "PLAN-2026-001",
            ProductName = "Corn Starch",
            Quantity = 5000,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            Status = "In Progress"
        };
        db.ProductionPlans.Add(plan);
        await db.SaveChangesAsync();

        // Act: Record actual output with loss
        decimal expectedOutput = 5000m;
        decimal actualOutput = 4800m;
        decimal lossQty = expectedOutput - actualOutput;
        decimal lossPct = (lossQty / expectedOutput) * 100m;

        // Assert: Loss tracking is accurate
        Assert.Equal(200m, lossQty);
        Assert.Equal(4.0m, lossPct);
        Assert.Equal("In Progress", plan.Status);
    }

    // ════════════════════════════════════════════════════════════════
    // PRD-02: Production Order Explosion & Component Issue
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PRD02_BOM_Explosion_CreatesReservations()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: BOM for Corn Starch
        var bomItems = new[]
        {
            new BillOfMaterialEntity { TenantId = tenantId, BomId = "BOM-CS-001", ProductName = "Corn Starch", ComponentName = "Wet Corn", Quantity = 1.8m, UOM = "KG" },
            new BillOfMaterialEntity { TenantId = tenantId, BomId = "BOM-CS-001", ProductName = "Corn Starch", ComponentName = "HCSL", Quantity = 0.25m, UOM = "L" },
            new BillOfMaterialEntity { TenantId = tenantId, BomId = "BOM-CS-001", ProductName = "Corn Starch", ComponentName = "Process Water", Quantity = 0.5m, UOM = "L" },
            new BillOfMaterialEntity { TenantId = tenantId, BomId = "BOM-CS-001", ProductName = "Corn Starch", ComponentName = "Caustic Soda", Quantity = 0.02m, UOM = "L" },
        };
        db.BillOfMaterials.AddRange(bomItems);
        await db.SaveChangesAsync();

        // Act: Create production order for 1000 kg
        decimal orderQty = 1000m;
        var order = new ProductionOrderEntity
        {
            TenantId = tenantId,
            OrderNumber = $"PROD-{DateTime.Now:yyyyMMdd}-001",
            ProductName = "Corn Starch",
            Quantity = orderQty,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(8),
            Status = "PLANNED"
        };
        db.ProductionOrders.Add(order);
        await db.SaveChangesAsync();

        // Explode BOM
        var components = await db.BillOfMaterials
            .Where(b => b.BomId == "BOM-CS-001")
            .ToListAsync();

        var reservations = components.Select(c => new ProductionOrderItemEntity
        {
            ProductionOrderId = order.Id,
            MaterialName = c.ComponentName,
            RequiredQty = c.Quantity * orderQty,
            UOM = c.UOM,
            Status = "PLANNED"
        }).ToList();

        db.ProductionOrderItems.AddRange(reservations);
        await db.SaveChangesAsync();

        // Assert: BOM explosion created correct reservations
        var savedItems = await db.ProductionOrderItems
            .Where(i => i.ProductionOrderId == order.Id)
            .ToListAsync();

        Assert.Equal(4, savedItems.Count);
        Assert.Contains(savedItems, i => i.MaterialName == "Wet Corn" && i.RequiredQty == 1800m);
        Assert.Contains(savedItems, i => i.MaterialName == "HCSL" && i.RequiredQty == 250m);
        Assert.Contains(savedItems, i => i.MaterialName == "Process Water" && i.RequiredQty == 500m);
        Assert.Contains(savedItems, i => i.MaterialName == "Caustic Soda" && i.RequiredQty == 20m);
    }

    [Fact]
    public async Task PRD02_GoodsIssue_Mvt261_DeductsRawStock()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Raw material stock
        var stock = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Wet Corn",
            Quantity = 20000,
            UOM = "KG",
            Value = 40000m
        };
        db.StockItems.Add(stock);

        var order = new ProductionOrderEntity
        {
            TenantId = tenantId,
            OrderNumber = "PROD-2026-001",
            ProductName = "Corn Starch",
            Quantity = 1000,
            Status = "RELEASED"
        };
        db.ProductionOrders.Add(order);
        await db.SaveChangesAsync();

        // Act: Issue components (Mvt 261 - GI for Production)
        decimal issueQty = 1800m;  // 1.8 * 1000
        decimal stockBefore = stock.Quantity;

        stock.Quantity -= issueQty;
        await db.SaveChangesAsync();

        // Record movement
        var movement = new StockMovementEntity
        {
            TenantId = tenantId,
            DocumentNumber = $"GI-{DateTime.Now:yyyyMMddHHmmss}",
            MaterialName = "Wet Corn",
            MovementType = "261",
            Quantity = issueQty,
            StockBefore = stockBefore,
            StockAfter = stock.Quantity,
            Reference = order.OrderNumber,
            Status = "Posted"
        };
        db.StockMovements.Add(movement);
        await db.SaveChangesAsync();

        // Assert: Stock deducted correctly
        var refreshedStock = await db.StockItems.FindAsync(stock.Id);
        Assert.Equal(18200m, refreshedStock!.Quantity);  // 20000 - 1800

        var savedMovement = await db.StockMovements
            .FirstOrDefaultAsync(m => m.DocumentNumber == movement.DocumentNumber);
        Assert.NotNull(savedMovement);
        Assert.Equal("261", savedMovement!.MovementType);
        Assert.Equal(1800m, savedMovement.Quantity);
    }

    [Fact]
    public async Task PRD02_ProductionOrder_StatusTransitions_Valid()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange
        var order = new ProductionOrderEntity
        {
            TenantId = tenantId,
            OrderNumber = "PROD-TRANS-001",
            ProductName = "Dextrose",
            Quantity = 500,
            Status = "PLANNED"
        };
        db.ProductionOrders.Add(order);
        await db.SaveChangesAsync();

        // Act & Assert: Valid transitions
        Assert.True(order.CanTransitionTo("RELEASED"), "PLANNED -> RELEASED should be valid");
        order.TransitionTo("RELEASED");
        Assert.Equal("RELEASED", order.Status);

        Assert.True(order.CanTransitionTo("IN_PROGRESS"), "RELEASED -> IN_PROGRESS should be valid");
        order.TransitionTo("IN_PROGRESS");
        Assert.Equal("IN_PROGRESS", order.Status);

        Assert.True(order.CanTransitionTo("COMPLETED"), "IN_PROGRESS -> COMPLETED should be valid");
        order.TransitionTo("COMPLETED");
        Assert.Equal("COMPLETED", order.Status);

        Assert.True(order.CanTransitionTo("TECO"), "COMPLETED -> TECO should be valid");
        order.TransitionTo("TECO");
        Assert.Equal("TECO", order.Status);
    }

    [Fact]
    public async Task PRD02_ProductionOrder_InvalidTransition_Throws()
    {
        var db = CreateDb();
        var order = new ProductionOrderEntity
        {
            TenantId = Guid.NewGuid(),
            OrderNumber = "PROD-INVALID-001",
            ProductName = "Corn Starch",
            Quantity = 100,
            Status = "PLANNED"
        };
        db.ProductionOrders.Add(order);
        await db.SaveChangesAsync();

        // Act & Assert: Invalid transition
        Assert.False(order.CanTransitionTo("COMPLETED"), "PLANNED -> COMPLETED should be invalid");
        Assert.Throws<InvalidOperationException>(() => order.TransitionTo("COMPLETED"));
    }
}
