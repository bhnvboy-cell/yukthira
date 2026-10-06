using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class ColumnarCacheTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private static ColumnarJournalCache CreateCache(YuktiraDbContext db, string? exportDirectory = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        var provider = services.BuildServiceProvider();

        return new ColumnarJournalCache(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ColumnarCacheOptions
            {
                Enabled = true,
                ExportDirectory = exportDirectory ?? "data/parquet"
            }));
    }

    private static async Task<int> SeedJournals(YuktiraDbContext db, int fiscalYear)
    {
        var tenantId = Guid.NewGuid();
        db.UniversalJournals.AddRange(
            new UniversalJournalEntity
            {
                TenantId = tenantId,
                FiscalYear = fiscalYear,
                Period = 1,
                DocumentNumber = "DOC-1",
                DocumentType = "SA",
                AccountCode = "400000",
                AccountType = "Revenue",
                Plant = "1000",
                CustomerCode = "C-001",
                DebitAmount = 100,
                CreditAmount = 0,
                AmountLC = 100,
                PostingDate = new DateTime(fiscalYear, 1, 15)
            },
            new UniversalJournalEntity
            {
                TenantId = tenantId,
                FiscalYear = fiscalYear,
                Period = 2,
                DocumentNumber = "DOC-2",
                DocumentType = "SA",
                AccountCode = "400000",
                AccountType = "Revenue",
                Plant = "1000",
                CustomerCode = "C-002",
                DebitAmount = 200,
                CreditAmount = 0,
                AmountLC = 200,
                PostingDate = new DateTime(fiscalYear, 2, 15)
            },
            new UniversalJournalEntity
            {
                TenantId = tenantId,
                FiscalYear = fiscalYear,
                Period = 1,
                DocumentNumber = "DOC-3",
                DocumentType = "SA",
                AccountCode = "500000",
                AccountType = "Expense",
                Plant = "2000",
                VendorCode = "V-001",
                DebitAmount = 0,
                CreditAmount = 150,
                AmountLC = -150,
                PostingDate = new DateTime(fiscalYear, 1, 20)
            },
            new UniversalJournalEntity
            {
                TenantId = tenantId,
                FiscalYear = fiscalYear,
                Period = 3,
                DocumentNumber = "DOC-4",
                DocumentType = "SA",
                AccountCode = "500000",
                AccountType = "Expense",
                Plant = "2000",
                VendorCode = "V-002",
                DebitAmount = 0,
                CreditAmount = 250,
                AmountLC = -250,
                PostingDate = new DateTime(fiscalYear, 3, 20)
            });
        await db.SaveChangesAsync();
        return 4;
    }

    [Fact]
    public async Task CC01_BuildAndGroup_ReturnsAggregatedMeasures()
    {
        var db = CreateDb();
        var expectedRows = await SeedJournals(db, 2026);
        var cache = CreateCache(db);

        var built = await cache.BuildAsync();

        Assert.Equal(expectedRows, built);
        Assert.Equal(expectedRows, cache.RowCount);
        Assert.NotNull(cache.LastBuiltAt);

        var result = await cache.QueryAsync(new ColumnarQuery
        {
            GroupBy = new List<string> { "AccountCode" },
            Measures = new List<string> { "sumDebit", "sumCredit", "sumAmountLC", "count" }
        });

        Assert.Equal("ColumnarCache", result.Engine);
        Assert.False(result.UsedFallback);
        Assert.Equal(expectedRows, result.MatchedRows);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(new List<string> { "AccountCode", "sumDebit", "sumCredit", "sumAmountLC", "count" }, result.Columns);

        var revenue = result.Rows.Single(r => (string?)r["AccountCode"] == "400000");
        Assert.Equal(300, (decimal)revenue["sumDebit"]!);
        Assert.Equal(0, (decimal)revenue["sumCredit"]!);
        Assert.Equal(300, (decimal)revenue["sumAmountLC"]!);
        Assert.Equal(2, (int)revenue["count"]!);

        var expense = result.Rows.Single(r => (string?)r["AccountCode"] == "500000");
        Assert.Equal(400, (decimal)expense["sumCredit"]!);
        Assert.Equal(-400, (decimal)expense["sumAmountLC"]!);
        Assert.Equal(2, (int)expense["count"]!);
    }

    [Fact]
    public async Task CC02_FiltersApply_AndUnknownColumnsAreRejected()
    {
        var db = CreateDb();
        await SeedJournals(db, 2026);
        var cache = CreateCache(db);
        await cache.BuildAsync();

        var filtered = await cache.QueryAsync(new ColumnarQuery
        {
            GroupBy = new List<string> { "Period" },
            Measures = new List<string> { "sumDebit", "count" },
            Filters = new List<ColumnarFilter>
            {
                new() { Field = "AccountCode", Op = "eq", Value = "400000" },
                new() { Field = "Period", Op = "gte", Value = "2" }
            }
        });

        Assert.Equal(1, filtered.MatchedRows);
        var row = Assert.Single(filtered.Rows);
        Assert.Equal("2", (string)row["Period"]!);
        Assert.Equal(200, (decimal)row["sumDebit"]!);
        Assert.Equal(1, (int)row["count"]!);

        var ranged = await cache.QueryAsync(new ColumnarQuery
        {
            GroupBy = new List<string> { "AccountCode" },
            Measures = new List<string> { "count" },
            Filters = new List<ColumnarFilter>
            {
                new() { Field = "PostingDate", Op = "between", Value = "2026-01-01", ValueTo = "2026-01-31" }
            }
        });
        Assert.Equal(2, ranged.MatchedRows);

        await Assert.ThrowsAsync<ArgumentException>(() => cache.QueryAsync(new ColumnarQuery
        {
            GroupBy = new List<string> { "NotAColumn" }
        }));
        await Assert.ThrowsAsync<ArgumentException>(() => cache.QueryAsync(new ColumnarQuery
        {
            Measures = new List<string> { "sumEverything" }
        }));
        await Assert.ThrowsAsync<ArgumentException>(() => cache.QueryAsync(new ColumnarQuery
        {
            Filters = new List<ColumnarFilter> { new() { Field = "AccountCode", Op = "like", Value = "4" } }
        }));
    }

    [Fact]
    public async Task CC03_Export_WritesReadableParquetFile()
    {
        var db = CreateDb();
        var fiscalYear = DateTime.UtcNow.Year;
        var expectedRows = await SeedJournals(db, fiscalYear);
        var directory = Path.Combine(Path.GetTempPath(), "yuktira-parquet-" + Guid.NewGuid().ToString("N"));

        try
        {
            var cache = CreateCache(db, directory);
            await cache.BuildAsync();

            var path = await cache.ExportAsync(fiscalYear);

            Assert.True(File.Exists(path));
            Assert.Equal($"uj_{fiscalYear}.parquet", Path.GetFileName(path));
            Assert.Equal(directory, Path.GetDirectoryName(path));

            var status = await cache.GetStatusAsync();
            Assert.True(status.Enabled);
            Assert.NotNull(status.LastExportAt);
            Assert.Equal("", status.LastError);
            Assert.Equal(expectedRows, status.TotalRows);

            var file = Assert.Single(status.Files);
            Assert.Equal(fiscalYear, file.FiscalYear);
            Assert.Equal(expectedRows, file.Rows);
            Assert.True(file.WrittenAt <= DateTime.UtcNow);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
