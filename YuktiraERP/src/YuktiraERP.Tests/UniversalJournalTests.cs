using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Tests;

public class UniversalJournalTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    [Fact]
    public async Task UNI01_PostJournal_BalancedDebitCredit()
    {
        var db = CreateDb();
        var service = new UniversalJournalService(db);

        var result = await service.PostAsync(new JournalPostRequest
        {
            CompanyCode = "1000",
            FiscalYear = "2026",
            DocumentDate = DateTime.UtcNow,
            PostingDate = DateTime.UtcNow,
            DocumentType = "SA",
            Reference = "Test Journal",
            LineItems = new List<JournalLineItemRequest>
            {
                new() { GlAccount = "1100", CostCenter = "CC01", DebitAmount = 5000, CreditAmount = 0, Currency = "INR", Description = "Cash Debit" },
                new() { GlAccount = "4100", CostCenter = "CC01", DebitAmount = 0, CreditAmount = 5000, Currency = "INR", Description = "Revenue Credit" }
            }
        });

        Assert.True(result.Success);
        Assert.NotEmpty(result.DocumentNumber);

        var journals = await db.UniversalJournals
            .Where(j => j.DocumentNumber == result.DocumentNumber)
            .ToListAsync();
        Assert.Equal(2, journals.Count);
        Assert.Equal(5000, journals[0].DebitAmount);
        Assert.Equal(5000, journals[1].CreditAmount);
        Assert.Equal("Posted", journals[0].Status);
    }

    [Fact]
    public async Task UNI02_PostBatch_MultipleEntries()
    {
        var db = CreateDb();
        var service = new UniversalJournalService(db);

        var result = await service.PostBatchAsync(new JournalBatchPostRequest
        {
            Journals = new List<JournalPostRequest>
            {
                new()
                {
                    CompanyCode = "1000", FiscalYear = "2026",
                    DocumentDate = DateTime.UtcNow, PostingDate = DateTime.UtcNow,
                    DocumentType = "SA",
                    LineItems = new List<JournalLineItemRequest>
                    {
                        new() { GlAccount = "1100", DebitAmount = 1000, CreditAmount = 0, Currency = "INR" },
                        new() { GlAccount = "4100", DebitAmount = 0, CreditAmount = 1000, Currency = "INR" }
                    }
                },
                new()
                {
                    CompanyCode = "1000", FiscalYear = "2026",
                    DocumentDate = DateTime.UtcNow, PostingDate = DateTime.UtcNow,
                    DocumentType = "SA",
                    LineItems = new List<JournalLineItemRequest>
                    {
                        new() { GlAccount = "1200", DebitAmount = 2000, CreditAmount = 0, Currency = "INR" },
                        new() { GlAccount = "4200", DebitAmount = 0, CreditAmount = 2000, Currency = "INR" }
                    }
                }
            }
        });

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
    }

    [Fact]
    public async Task UNI03_ReverseJournal_CreatesReversal()
    {
        var db = CreateDb();
        var service = new UniversalJournalService(db);

        var postResult = await service.PostAsync(new JournalPostRequest
        {
            CompanyCode = "1000", FiscalYear = "2026",
            DocumentDate = DateTime.UtcNow, PostingDate = DateTime.UtcNow,
            DocumentType = "SA",
            LineItems = new List<JournalLineItemRequest>
            {
                new() { GlAccount = "1100", DebitAmount = 3000, CreditAmount = 0, Currency = "INR" },
                new() { GlAccount = "4100", DebitAmount = 0, CreditAmount = 3000, Currency = "INR" }
            }
        });
        Assert.True(postResult.Success);

        var reverseResult = await service.ReverseAsync(new JournalReverseRequest
        {
            DocumentNumber = postResult.DocumentNumber,
            FiscalYear = "2026",
            PostingDate = DateTime.UtcNow,
            Reason = "Error correction",
            ReverseOriginal = true
        });

        Assert.True(reverseResult.Success);
        Assert.NotEmpty(reverseResult.ReversalDocumentNumber);

        var reversalEntries = await db.UniversalJournals
            .Where(j => j.DocumentNumber == reverseResult.ReversalDocumentNumber)
            .ToListAsync();
        Assert.Equal(2, reversalEntries.Count);
        Assert.True(reversalEntries.All(j => j.IsReversal));
        Assert.Equal("REVERSAL", reversalEntries[0].DocumentType);

        var originalEntries = await db.UniversalJournals
            .Where(j => j.DocumentNumber == postResult.DocumentNumber)
            .ToListAsync();
        Assert.True(originalEntries.All(j => j.Status == "Reversed"));
    }

    [Fact]
    public async Task UNI04_TrialBalance_CorrectTotals()
    {
        var db = CreateDb();
        var service = new UniversalJournalService(db);

        await service.PostAsync(new JournalPostRequest
        {
            CompanyCode = "1000", FiscalYear = "2026",
            DocumentDate = DateTime.UtcNow, PostingDate = DateTime.UtcNow,
            DocumentType = "SA",
            LineItems = new List<JournalLineItemRequest>
            {
                new() { GlAccount = "1100", DebitAmount = 10000, CreditAmount = 0, Currency = "INR" },
                new() { GlAccount = "4100", DebitAmount = 0, CreditAmount = 10000, Currency = "INR" }
            }
        });

        await service.PostAsync(new JournalPostRequest
        {
            CompanyCode = "1000", FiscalYear = "2026",
            DocumentDate = DateTime.UtcNow, PostingDate = DateTime.UtcNow,
            DocumentType = "SA",
            LineItems = new List<JournalLineItemRequest>
            {
                new() { GlAccount = "1100", DebitAmount = 5000, CreditAmount = 0, Currency = "INR" },
                new() { GlAccount = "4100", DebitAmount = 0, CreditAmount = 5000, Currency = "INR" }
            }
        });

        var trialBalance = await service.GetTrialBalanceAsync(new TrialBalanceRequest
        {
            CompanyCode = "1000",
            FiscalYear = "2026",
            FiscalPeriod = 12
        });

        Assert.Equal(12, trialBalance.FiscalPeriod);
        Assert.True(trialBalance.IsBalanced);
        Assert.Equal(trialBalance.TotalDebit, trialBalance.TotalCredit);
        Assert.True(trialBalance.TotalDebit >= 15000);
    }

    [Fact]
    public async Task UNI05_CostCenterReport_GroupsCorrectly()
    {
        var db = CreateDb();
        var service = new UniversalJournalService(db);

        await service.PostAsync(new JournalPostRequest
        {
            CompanyCode = "1000", FiscalYear = "2026",
            DocumentDate = DateTime.UtcNow, PostingDate = DateTime.UtcNow,
            DocumentType = "SA",
            LineItems = new List<JournalLineItemRequest>
            {
                new() { GlAccount = "5100", CostCenter = "CC-SALES", DebitAmount = 3000, CreditAmount = 0, Currency = "INR" },
                new() { GlAccount = "4100", CostCenter = "CC-SALES", DebitAmount = 0, CreditAmount = 3000, Currency = "INR" }
            }
        });

        await service.PostAsync(new JournalPostRequest
        {
            CompanyCode = "1000", FiscalYear = "2026",
            DocumentDate = DateTime.UtcNow, PostingDate = DateTime.UtcNow,
            DocumentType = "SA",
            LineItems = new List<JournalLineItemRequest>
            {
                new() { GlAccount = "5200", CostCenter = "CC-ADMIN", DebitAmount = 1500, CreditAmount = 0, Currency = "INR" },
                new() { GlAccount = "4200", CostCenter = "CC-ADMIN", DebitAmount = 0, CreditAmount = 1500, Currency = "INR" }
            }
        });

        var report = await service.GetCostCenterReportAsync(new CostCenterReportRequest
        {
            CompanyCode = "1000",
            FiscalYear = "2026",
            PeriodFrom = 1,
            PeriodTo = 12
        });

        Assert.Equal(2, report.LineItems.Count);
        Assert.Contains(report.LineItems, l => l.CostCenter == "CC-SALES");
        Assert.Contains(report.LineItems, l => l.CostCenter == "CC-ADMIN");
        Assert.Equal(4500, report.TotalActual);
    }
}
