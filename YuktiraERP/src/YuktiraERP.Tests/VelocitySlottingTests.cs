using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Tests;

public class VelocitySlottingTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task SLO01_CalculateVelocity_ABCDClass()
    {
        var db = CreateDb();
        var service = new VelocitySlottingService(db);

        var materials = new[]
        {
            new VelocitySlottingEntity { MaterialCode = "MAT-FAST", MaterialName = "Fast Mover", Plant = "P1", Status = "Active", ConsumptionQty30Day = 500, CurrentBin = "A-01-01", CurrentZone = "A", VelocityClass = "C" },
            new VelocitySlottingEntity { MaterialCode = "MAT-MED", MaterialName = "Medium Mover", Plant = "P1", Status = "Active", ConsumptionQty30Day = 200, CurrentBin = "A-02-01", CurrentZone = "A", VelocityClass = "C" },
            new VelocitySlottingEntity { MaterialCode = "MAT-SLOW", MaterialName = "Slow Mover", Plant = "P1", Status = "Active", ConsumptionQty30Day = 50, CurrentBin = "A-03-01", CurrentZone = "A", VelocityClass = "C" },
            new VelocitySlottingEntity { MaterialCode = "MAT-XSLOW", MaterialName = "Extra Slow", Plant = "P1", Status = "Active", ConsumptionQty30Day = 10, CurrentBin = "A-04-01", CurrentZone = "A", VelocityClass = "C" },
        };
        db.VelocitySlottings.AddRange(materials);
        await db.SaveChangesAsync();

        var result = await service.CalculateVelocityClassesAsync(new VelocityClassCalculationRequest
        {
            PlantId = "P1",
            StorageLocation = "WH-01",
            NumberOfClasses = 4
        });

        Assert.True(result.Success);
        Assert.Equal(4, result.MaterialsProcessed);
        Assert.True(result.VelocityClasses.Count > 0);
        Assert.Equal("A", result.VelocityClasses.First().ClassCode);

        var fastMaterial = await db.VelocitySlottings.FirstOrDefaultAsync(v => v.MaterialCode == "MAT-FAST");
        Assert.Equal("A", fastMaterial.VelocityClass);
        Assert.Equal("PICK_FAST", fastMaterial.RecommendedZone);

        var slowMaterial = await db.VelocitySlottings.FirstOrDefaultAsync(v => v.MaterialCode == "MAT-XSLOW");
        Assert.Contains(slowMaterial.VelocityClass, new[] { "C", "D" });
    }

    [Fact]
    public async Task SLO02_GetRecommendations_ReturnsBins()
    {
        var db = CreateDb();
        var service = new VelocitySlottingService(db);

        db.VelocitySlottings.Add(new VelocitySlottingEntity
        {
            MaterialCode = "MAT-001", MaterialName = "Widget", Plant = "P1",
            VelocityClass = "A", CurrentBin = "C-05-03", CurrentZone = "BULK",
            RecommendedBin = "A-01-01", RecommendedZone = "PICK_FAST", Status = "Active"
        });
        db.VelocitySlottings.Add(new VelocitySlottingEntity
        {
            MaterialCode = "MAT-002", MaterialName = "Gadget", Plant = "P1",
            VelocityClass = "B", CurrentBin = "B-02-01", CurrentZone = "PICK_MEDIUM",
            RecommendedBin = "B-01-01", RecommendedZone = "PICK_MEDIUM", Status = "Active"
        });
        await db.SaveChangesAsync();

        var result = await service.GetRecommendationsAsync(new SlottingRecommendationRequest
        {
            PlantId = "P1",
            VelocityClass = "A"
        });

        Assert.True(result.Recommendations.Count > 0);
        var rec = result.Recommendations.First();
        Assert.Equal("MAT-001", rec.MaterialNumber);
        Assert.Equal("A", rec.CurrentVelocityClass);
        Assert.Equal("PICK_FAST", rec.RecommendedZone);
        Assert.Contains(rec.ReasonCode, new[] { "VELOCITY_RECLASS", "OPTIMAL" });
    }

    [Fact]
    public async Task SLO03_ApplySlotting_UpdatesCurrentBin()
    {
        var db = CreateDb();
        var service = new VelocitySlottingService(db);

        db.VelocitySlottings.Add(new VelocitySlottingEntity
        {
            MaterialCode = "MAT-001", MaterialName = "Widget", Plant = "P1",
            VelocityClass = "A", CurrentBin = "C-05-03", CurrentZone = "BULK",
            RecommendedZone = "PICK_FAST", Status = "Active"
        });
        await db.SaveChangesAsync();

        var result = await service.ApplySlottingAsync(new SlottingApplyRequest
        {
            PlantId = "P1",
            Changes = new System.Collections.Generic.List<SlottingChangeItem>
            {
                new() { MaterialNumber = "MAT-001", SourceBin = "C-05-03", DestinationBin = "A-01-01" }
            },
            ValidateOnly = false,
            Reason = "Velocity reclassification"
        });

        Assert.True(result.Success);
        Assert.Equal(1, result.ChangesApplied);
        Assert.Empty(result.Errors);

        var updated = await db.VelocitySlottings.FirstOrDefaultAsync(v => v.MaterialCode == "MAT-001");
        Assert.Equal("A-01-01", updated.CurrentBin);
        Assert.Equal("A", updated.CurrentZone);
        Assert.Contains("Applied slotting change", updated.Notes);
    }
}
