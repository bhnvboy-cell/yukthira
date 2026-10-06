using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class SelfHealingReconciliationTests
{
    private static YuktiraDbContext CreateInMemoryDb(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new YuktiraDbContext(options);
    }

    private static SelfHealingReconciliationService CreateService(YuktiraDbContext db, SelfHealingOptions? options = null)
    {
        var movement = new Mock<IInventoryMovementService>();
        return new SelfHealingReconciliationService(
            db,
            movement.Object,
            new AuditService(db),
            Options.Create(options ?? new SelfHealingOptions()));
    }

    private static UniversalJournalEntity Journal(
        Guid tenantId,
        string documentNumber,
        int lineNumber,
        string accountCode,
        string accountName,
        decimal debit,
        decimal credit,
        string reference = "",
        string vendorCode = "",
        string materialCode = "")
    {
        return new UniversalJournalEntity
        {
            TenantId = tenantId,
            FiscalYear = 2026,
            Period = 1,
            DocumentNumber = documentNumber,
            DocumentType = "SA",
            DocumentDate = DateTime.UtcNow,
            PostingDate = DateTime.UtcNow,
            LineNumber = lineNumber,
            AccountCode = accountCode,
            AccountName = accountName,
            AccountType = "BalanceSheet",
            DebitAmount = debit,
            CreditAmount = credit,
            Currency = "INR",
            AmountLC = debit - credit,
            Reference = reference,
            VendorCode = vendorCode,
            MaterialCode = materialCode,
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };
    }

    private static string Sha256Hex(string data)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private class StuckHeaderSeedContext : DbContext
    {
        public StuckHeaderSeedContext(DbContextOptions<StuckHeaderSeedContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(YuktiraDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }

    [Fact]
    public async Task SH01_PennyDelta_AppliesRoundingLine49999_AndRecordsAudit()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.UniversalJournals.AddRange(
            Journal(tenantId, "PEN-1", 1, "10000", "Cash", 100.00m, 0m),
            Journal(tenantId, "PEN-1", 2, "40000", "Revenue", 0m, 99.98m));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.RunOnceAsync(tenantId, Guid.NewGuid().ToString());

        var rounding = await db.UniversalJournals.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.AccountCode == "49999")
            .ToListAsync();
        var line = Assert.Single(rounding);
        Assert.Equal(0.02m, line.CreditAmount);
        Assert.StartsWith("SHPEN-", line.DocumentNumber);
        Assert.Equal("Rounding Off", line.AccountName);
        Assert.Equal(1, result.AppliedCount);

        var auditRow = await db.SxAudits.AsNoTracking().SingleAsync(a => a.TenantId == tenantId);
        Assert.Equal("PennyRounding", auditRow.ActionCategory);
        Assert.Equal("Applied", auditRow.Status);
        Assert.Equal("GENESIS", auditRow.PreviousHash);
        Assert.Equal(64, auditRow.CurrentHash.Length);
        Assert.Equal(auditRow.CurrentHash, Assert.Single(result.Actions).CurrentHash);
        Assert.Equal(1L, auditRow.SequenceNumber);
    }

    [Fact]
    public async Task SH02_GrIrImbalance_AppliesBalancingJournalLine()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.UniversalJournals.AddRange(
            Journal(tenantId, "GIR-A-1", 1, "191100", "GR/IR Clearing", 100m, 0m, "PO-100", "V-100", "M-100"),
            Journal(tenantId, "GIR-A-2", 1, "191100", "GR/IR Clearing", 0m, 90m, "PO-100", "V-100", "M-100"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.RunOnceAsync(tenantId, Guid.NewGuid().ToString());

        var balancing = await db.UniversalJournals.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.DocumentNumber.StartsWith("SHGIR-"))
            .ToListAsync();
        var line = Assert.Single(balancing);
        Assert.Equal(10m, line.CreditAmount);
        Assert.Equal(0m, line.DebitAmount);
        Assert.Equal(1, result.AppliedCount);
        var action = Assert.Single(result.Actions);
        Assert.Equal("GrIrReconciliation", action.Category);
        Assert.Equal("Applied", action.Status);
        Assert.Equal(10m, action.DeltaAmount);
    }

    [Fact]
    public async Task SH03_GrIrExcessiveDelta_FlagsWithoutPosting()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.UniversalJournals.AddRange(
            Journal(tenantId, "GIR-B-1", 1, "191100", "GR/IR Clearing", 5000m, 0m, "PO-200", "V-200", "M-200"),
            Journal(tenantId, "GIR-B-2", 1, "191100", "GR/IR Clearing", 0m, 1000m, "PO-200", "V-200", "M-200"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.RunOnceAsync(tenantId, Guid.NewGuid().ToString());

        var balancing = await db.UniversalJournals.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.DocumentNumber.StartsWith("SHGIR-"))
            .ToListAsync();
        Assert.Empty(balancing);
        Assert.Equal(0, result.AppliedCount);
        Assert.Equal(1, result.FlaggedCount);
        var action = Assert.Single(result.Actions);
        Assert.Equal("Flagged", action.Status);
        Assert.Equal(4000m, action.DeltaAmount);
    }

    [Fact]
    public async Task SH04_BalancedGrIrGroup_SkipsWithoutPosting()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.UniversalJournals.AddRange(
            Journal(tenantId, "GIR-C-1", 1, "191100", "GR/IR Clearing", 50m, 0m, "PO-300", "V-300", "M-300"),
            Journal(tenantId, "GIR-C-2", 1, "191100", "GR/IR Clearing", 0m, 50m, "PO-300", "V-300", "M-300"));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.RunOnceAsync(tenantId, Guid.NewGuid().ToString());

        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.AppliedCount);
        var balancing = await db.UniversalJournals.AsNoTracking()
            .Where(j => j.TenantId == tenantId && j.DocumentNumber.StartsWith("SHGIR-"))
            .ToListAsync();
        Assert.Empty(balancing);
        Assert.Equal("Skipped", Assert.Single(result.Actions).Status);
    }

    [Fact]
    public async Task SH05_StuckPostingWithoutSafeRetry_FlagsAndResolvesHeader()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var db = CreateInMemoryDb(databaseName);
        var tenantId = Guid.NewGuid();
        var tenantKey = tenantId.ToString();
        var seedOptions = new DbContextOptionsBuilder<StuckHeaderSeedContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var seedDb = new StuckHeaderSeedContext(seedOptions);
        seedDb.Set<MaterialDocumentHeaderEntity>().Add(new MaterialDocumentHeaderEntity
        {
            TenantId = tenantKey,
            DocumentNumber = "MB-STUCK-1",
            MovementType = 322,
            Status = "Pending",
            Plant = "1000",
            CreatedAt = DateTime.UtcNow.AddMinutes(-120)
        });
        await seedDb.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.RunOnceAsync(tenantId, Guid.NewGuid().ToString());

        Assert.Equal(1, result.StuckPostingsScanned);
        Assert.Equal(1, result.FlaggedCount);
        var header = await db.MaterialDocumentHeaders.AsNoTracking().SingleAsync(h => h.DocumentNumber == "MB-STUCK-1");
        Assert.Equal("Resolved", header.Status);
        Assert.True(header.CreatedAt < DateTime.UtcNow.AddMinutes(-60));
        var action = Assert.Single(result.Actions);
        Assert.Equal("StuckPosting", action.Category);
        Assert.Equal("Flagged", action.Status);
    }

    [Fact]
    public async Task SH06_RecentStuckPosting_IsNotTouched()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.MaterialDocumentHeaders.Add(new MaterialDocumentHeaderEntity
        {
            TenantId = tenantId.ToString(),
            DocumentNumber = "MB-RECENT-1",
            MovementType = 324,
            Status = "InProcess",
            Plant = "1000"
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.RunOnceAsync(tenantId, Guid.NewGuid().ToString());

        Assert.Equal(0, result.StuckPostingsScanned);
        Assert.Empty(result.Actions);
        var header = await db.MaterialDocumentHeaders.AsNoTracking().SingleAsync(h => h.DocumentNumber == "MB-RECENT-1");
        Assert.Equal("InProcess", header.Status);
        Assert.Empty(db.SxAudits);
    }

    [Fact]
    public async Task SH07_MultipleActions_ProduceContiguousHashChain()
    {
        await using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        db.UniversalJournals.AddRange(
            Journal(tenantId, "GIR-D-1", 1, "191100", "GR/IR Clearing", 50m, 0m, "PO-400", "V-400", "M-400"),
            Journal(tenantId, "GIR-D-2", 1, "191100", "GR/IR Clearing", 0m, 50m, "PO-400", "V-400", "M-400"),
            Journal(tenantId, "GIR-E-1", 1, "191100", "GR/IR Clearing", 5000m, 0m, "PO-500", "V-500", "M-500"),
            Journal(tenantId, "GIR-E-2", 1, "191100", "GR/IR Clearing", 0m, 1000m, "PO-500", "V-500", "M-500"),
            Journal(tenantId, "PEN-9", 1, "10000", "Cash", 75.00m, 0m),
            Journal(tenantId, "PEN-9", 2, "40000", "Revenue", 0m, 74.97m));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.RunOnceAsync(tenantId, Guid.NewGuid().ToString());

        Assert.Equal(3, result.Actions.Count);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(1, result.FlaggedCount);
        Assert.Equal(1, result.AppliedCount);
        Assert.Equal(new long[] { 1, 2, 3 }, result.Actions.Select(a => a.SequenceNumber).ToArray());
        Assert.Equal("GENESIS", result.Actions[0].PreviousHash);

        for (var i = 1; i < result.Actions.Count; i++)
        {
            Assert.Equal(result.Actions[i - 1].CurrentHash, result.Actions[i].PreviousHash);
        }

        foreach (var action in result.Actions)
        {
            var payload = $"{action.SequenceNumber}|{action.Category}|{action.TargetId}|{action.Summary}|{action.DeltaAmount}|{action.PreviousHash}";
            Assert.Equal(Sha256Hex(payload), action.CurrentHash);
            Assert.Equal(64, action.CurrentHash.Length);
        }

        var auditRows = await db.SxAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.SequenceNumber)
            .ToListAsync();
        Assert.Equal(3, auditRows.Count);
        Assert.Equal(new long[] { 1, 2, 3 }, auditRows.Select(a => a.SequenceNumber).ToArray());
        for (var i = 1; i < auditRows.Count; i++)
        {
            Assert.Equal(auditRows[i - 1].CurrentHash, auditRows[i].PreviousHash);
        }
        for (var i = 0; i < result.Actions.Count; i++)
        {
            Assert.Equal(auditRows[i].CurrentHash, result.Actions[i].CurrentHash);
        }
    }
}
