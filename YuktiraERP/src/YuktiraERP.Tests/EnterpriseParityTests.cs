using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class EnterpriseParityTests
{
    private YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task IntercompanyReconciliation_MatchesAndCreatesEliminationEntries()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();

        // Company 1000 has receivable of 50,000 from Company 2000
        db.JournalEntries.Add(new JournalEntryEntity
        {
            Id = Guid.NewGuid(),
            DocumentNumber = "IC-1000-REC-01",
            EntryDate = DateTime.UtcNow,
            Account = "IC-Receivable-2000",
            Debit = 50000m,
            Credit = 0m,
            Reference = "Intercompany Sale to 2000"
        });

        // Company 2000 has matching payable of 50,000 to Company 1000
        db.JournalEntries.Add(new JournalEntryEntity
        {
            Id = Guid.NewGuid(),
            DocumentNumber = "IC-2000-PAY-01",
            EntryDate = DateTime.UtcNow,
            Account = "IC-Payable-1000",
            Debit = 0m,
            Credit = 50000m,
            Reference = "Intercompany Purchase from 1000"
        });

        await db.SaveChangesAsync();

        var service = new IntercompanyAccountingService(db, NullLogger<IntercompanyAccountingService>.Instance);
        var reconciliations = await service.ReconcileIntercompanyAsync("1000", 2026, 10, tenantId);

        Assert.Single(reconciliations);
        Assert.True(reconciliations[0].Success);
        Assert.Equal(50000m, reconciliations[0].LocalAmount);

        // Verify elimination entry created in journal
        var elimEntries = await db.JournalEntries
            .Where(j => j.Account.StartsWith("IC-Elimination"))
            .ToListAsync();
        Assert.Single(elimEntries);
        Assert.Equal(50000m, elimEntries[0].Debit);
        Assert.Equal(50000m, elimEntries[0].Credit);
    }

    [Fact]
    public async Task DdmrpBufferService_CalculatesBuffersAndReplenishmentAccurately()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();

        db.MaterialMasters.Add(new MaterialMasterEntity
        {
            Id = Guid.NewGuid(),
            Code = "MAT-STERILE-VIAL",
            Name = "Sterile Glass Vial 10ml"
        });

        // Stock movements: 4,500 units consumed in past 90 days => ADU = 50 units/day
        db.StockMovements.Add(new StockMovementEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MaterialCode = "MAT-STERILE-VIAL",
            Quantity = 4500m,
            MovementType = "201",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        });

        // Current low stock: 80 units on hand, 0 open supply
        db.StockBalances.Add(new StockBalanceEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MaterialCode = "MAT-STERILE-VIAL",
            MaterialName = "Sterile Glass Vial 10ml",
            Plant = "1000",
            StorageLocation = "SL01",
            Quantity = 80m
        });

        await db.SaveChangesAsync();

        var service = new DdmrpBufferService(db);
        var buffer = await service.CalculateBufferZonesAsync(tenantId, "MAT-STERILE-VIAL", "1000");

        Assert.Equal(50m, buffer.AverageDailyUsage);
        Assert.Equal(7, buffer.DecoupledLeadTimeDays);
        Assert.Equal(350m, buffer.YellowZone); // 50 * 7
        Assert.True(buffer.RedZone > 0m);
        Assert.True(buffer.GreenZone > 0m);

        // Since on-hand (80) is below TopOfRed, status must be Critical-Red
        Assert.Equal("Critical-Red", buffer.BufferStatus);
        Assert.True(buffer.RecommendedOrderQuantity > 0m);
    }

    [Fact]
    public async Task CrossDockService_MatchesOutboundDemandAndAssignsStagingLane()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();

        // Outbound sales order pending delivery for MAT-CR-100
        db.SalesOrderLines.Add(new SalesOrderLineEntity
        {
            Id = Guid.NewGuid(),
            MaterialName = "MAT-CR-100",
            Quantity = 250m
        });

        await db.SaveChangesAsync();

        var service = new CrossDockService(db, NullLogger<CrossDockService>.Instance);
        var result = await service.EvaluateCrossDockAsync(new CrossDockRequest
        {
            TenantId = tenantId,
            MaterialCode = "MAT-CR-100",
            Plant = "1000",
            Quantity = 300m
        });

        Assert.True(result.Success);
        Assert.True(result.EligibleForCrossDock);
        Assert.NotNull(result.AssignedBin);
        Assert.StartsWith("XDOCK-", result.AssignedBin);
        Assert.Equal(250m, result.DemandMatchQuantity);
    }
}
