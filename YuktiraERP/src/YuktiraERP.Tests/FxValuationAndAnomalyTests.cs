using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class FxValuationAndAnomalyTests
{
    private YuktiraDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task FxValuation_CalculatesUnrealizedGainAndPostsBalancingEntries()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var valuationDate = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc);

        // Historical foreign currency entry: 1,000 USD booked at historical rate 80.00 INR = 80,000 INR
        db.UniversalJournals.Add(new UniversalJournalEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentNumber = "INV-USD-001",
            DocumentType = "DR",
            PostingDate = valuationDate.AddDays(-15),
            AccountCode = "120100",
            AccountName = "Trade Receivables USD",
            DebitAmount = 1000m,
            CreditAmount = 0m,
            Currency = "USD",
            ExchangeRate = 80.00m,
            AmountLC = 80000m,
            IsReversal = false
        });

        // Closing exchange rate: 1 USD = 84.00 INR (gain of 4,000 INR)
        db.ExchangeRates.Add(new ExchangeRateEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FromCurrency = "USD",
            ToCurrency = "INR",
            Rate = 84.00m,
            EffectiveFrom = valuationDate.AddDays(-1)
        });

        await db.SaveChangesAsync();

        var service = new FxValuationService(db);

        // 1. Preview
        var preview = await service.PreviewRevaluationAsync(tenantId, "INR", valuationDate);
        Assert.Single(preview);
        Assert.Equal("Gain", preview[0].ValuationStatus);
        Assert.Equal(80000m, preview[0].BookValueLC);
        Assert.Equal(84000m, preview[0].RevaluedValueLC);
        Assert.Equal(4000m, preview[0].UnrealizedDeltaLC);

        // 2. Execute and Post Entries
        var result = await service.ExecuteRevaluationAsync(tenantId, "INR", valuationDate, postEntries: true, "test-user");
        Assert.True(result.EntriesPosted);
        Assert.Equal(4000m, result.TotalUnrealizedGain);
        Assert.Equal(0m, result.TotalUnrealizedLoss);
        Assert.Equal(4000m, result.NetUnrealizedImpact);

        // Verify balancing entries in database
        var postedEntries = await db.UniversalJournals
            .Where(j => j.DocumentNumber == result.JournalDocumentNumber)
            .ToListAsync();
        Assert.Equal(2, postedEntries.Count);
        Assert.Equal(4000m, postedEntries.Sum(e => e.DebitAmount));
        Assert.Equal(4000m, postedEntries.Sum(e => e.CreditAmount));
    }

    [Fact]
    public async Task FinancialAnomaly_DetectsStatisticalOutlierAndRoundSum()
    {
        using var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var date = DateTime.UtcNow;

        // Normal distribution around 1,000 - 2,000
        for (int i = 0; i < 20; i++)
        {
            db.UniversalJournals.Add(new UniversalJournalEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                DocumentNumber = $"DOC-{i}",
                PostingDate = date.AddDays(-i),
                AccountCode = "400100",
                AccountName = "Operating Expense",
                DebitAmount = 1000m + (i * 20),
                CreditAmount = 0m,
                Currency = "INR",
                IsReversal = false
            });
        }

        // Outlier amount of 50,000
        db.UniversalJournals.Add(new UniversalJournalEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DocumentNumber = "OUTLIER-01",
            PostingDate = date,
            AccountCode = "400100",
            AccountName = "Operating Expense",
            DebitAmount = 50000m,
            CreditAmount = 0m,
            Currency = "INR",
            IsReversal = false
        });

        await db.SaveChangesAsync();

        var service = new FinancialAnomalyService(db);
        var report = await service.ScanJournalAnomaliesAsync(tenantId, date.AddDays(-30), date.AddDays(1), thresholdZ: 2.0);

        Assert.True(report.TotalAnomalies >= 1);
        var outlier = report.Anomalies.FirstOrDefault(a => a.DocumentNumber == "OUTLIER-01");
        Assert.NotNull(outlier);
        Assert.True(outlier.ZScore >= 2.0);
    }
}
