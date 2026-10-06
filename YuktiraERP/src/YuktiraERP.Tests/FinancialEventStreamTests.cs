using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Messaging;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class FinancialEventStreamTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private static FinancialEventStream CreateStream(YuktiraDbContext db, FinancialEventStreamOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IUniversalJournalService>(new UniversalJournalService(db));
        var provider = services.BuildServiceProvider();

        return new FinancialEventStream(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options ?? new FinancialEventStreamOptions()));
    }

    private static string JournalPayload(decimal debit, decimal credit)
    {
        return JsonSerializer.Serialize(new JournalPostRequest
        {
            CompanyCode = "1000",
            FiscalYear = "2026",
            DocumentDate = DateTime.UtcNow,
            PostingDate = DateTime.UtcNow,
            DocumentType = "SA",
            Reference = "FS-TEST",
            LineItems = new List<JournalLineItemRequest>
            {
                new() { GlAccount = "400000", DebitAmount = debit, CreditAmount = 0, Currency = "INR", Description = "Debit line" },
                new() { GlAccount = "410000", DebitAmount = 0, CreditAmount = credit, Currency = "INR", Description = "Credit line" }
            }
        });
    }

    private static string ComputeHash(long sequence, string eventType, string payload, string previousHash)
    {
        var raw = $"{sequence}|{eventType}|{payload}|{previousHash}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
    }

    [Fact]
    public async Task FS01_Enqueue_PersistsHashChainInSequenceOrder()
    {
        var db = CreateDb();
        var stream = CreateStream(db);
        var tenantId = Guid.NewGuid();
        var streamId = Guid.NewGuid();

        var first = await stream.EnqueueAsync(new FinancialEventInput
        {
            TenantId = tenantId,
            StreamId = streamId,
            EventType = "JournalPosted",
            Payload = "{}",
            CorrelationId = "corr-1"
        });
        var second = await stream.EnqueueAsync(new FinancialEventInput
        {
            TenantId = tenantId,
            StreamId = streamId,
            EventType = "JournalPosted",
            Payload = "{\"Sequence\":2}",
            CorrelationId = "corr-2"
        });

        Assert.Equal(1, first.Sequence);
        Assert.Equal(2, second.Sequence);
        Assert.Equal("Pending", first.Status);
        Assert.Equal("UniversalJournal", first.StreamType);
        Assert.Equal("GENESIS", first.PreviousHash);
        Assert.Equal(first.Hash, second.PreviousHash);
        Assert.Equal(64, first.Hash.Length);
        Assert.Equal(64, second.Hash.Length);
        Assert.Equal(ComputeHash(1, "JournalPosted", "{}", "GENESIS"), first.Hash);
        Assert.Equal(
            ComputeHash(2, "JournalPosted", "{\"Sequence\":2}", first.Hash),
            second.Hash);
        Assert.Equal(2, stream.ChannelDepth);

        var rows = await db.FinancialEvents
            .Where(e => e.StreamId == streamId)
            .OrderBy(e => e.Sequence)
            .ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal("Pending", row.Status));
        Assert.Equal("GENESIS", rows[0].PreviousHash);
        Assert.Equal(rows[0].Hash, rows[1].PreviousHash);
    }

    [Fact]
    public async Task FS02_DrainPending_AppliesBalancedJournal()
    {
        var db = CreateDb();
        var stream = CreateStream(db);
        var tenantId = Guid.NewGuid();
        var streamId = Guid.NewGuid();

        var envelope = await stream.EnqueueAsync(new FinancialEventInput
        {
            TenantId = tenantId,
            StreamId = streamId,
            EventType = "JournalPosted",
            Payload = JournalPayload(1000, 1000)
        });

        var processed = await stream.DrainPendingAsync();

        Assert.Equal(1, processed);
        Assert.Equal(0, stream.ChannelDepth);

        var entity = await db.FinancialEvents.SingleAsync(e => e.Id == envelope.EventId);
        Assert.Equal("Applied", entity.Status);
        Assert.NotNull(entity.AppliedAt);
        Assert.Equal("", entity.Error);

        var journals = await db.UniversalJournals.ToListAsync();
        Assert.Equal(2, journals.Count);
        Assert.Equal(1000, journals.Sum(j => j.DebitAmount));
        Assert.Equal(1000, journals.Sum(j => j.CreditAmount));
        Assert.All(journals, j => Assert.Equal("Posted", j.Status));
    }

    [Fact]
    public async Task FS03_DrainPending_MarksRejectedEventsFailed()
    {
        var db = CreateDb();
        var stream = CreateStream(db);
        var tenantId = Guid.NewGuid();
        var streamId = Guid.NewGuid();

        var imbalanced = await stream.EnqueueAsync(new FinancialEventInput
        {
            TenantId = tenantId,
            StreamId = streamId,
            EventType = "JournalPosted",
            Payload = JournalPayload(500, 400)
        });
        var unsupported = await stream.EnqueueAsync(new FinancialEventInput
        {
            TenantId = tenantId,
            StreamId = streamId,
            EventType = "WorkflowApproved",
            Payload = "{}"
        });

        var processed = await stream.DrainPendingAsync();

        Assert.Equal(2, processed);

        var imbalancedEntity = await db.FinancialEvents.SingleAsync(e => e.Id == imbalanced.EventId);
        Assert.Equal("Failed", imbalancedEntity.Status);
        Assert.Contains("imbalance", imbalancedEntity.Error);

        var unsupportedEntity = await db.FinancialEvents.SingleAsync(e => e.Id == unsupported.EventId);
        Assert.Equal("Failed", unsupportedEntity.Status);
        Assert.Contains("Unsupported event type", unsupportedEntity.Error);

        Assert.Empty(await db.UniversalJournals.ToListAsync());
    }

    [Fact]
    public async Task FS04_Replay_RebuildsBalancedStreamTotals()
    {
        var db = CreateDb();
        var stream = CreateStream(db);
        var tenantId = Guid.NewGuid();
        var streamId = Guid.NewGuid();

        await stream.EnqueueAsync(new FinancialEventInput
        {
            TenantId = tenantId,
            StreamId = streamId,
            EventType = "JournalPosted",
            Payload = JournalPayload(2500, 2500)
        });
        await stream.EnqueueAsync(new FinancialEventInput
        {
            TenantId = tenantId,
            StreamId = streamId,
            EventType = "JournalPosted",
            Payload = JournalPayload(900, 100)
        });
        await stream.DrainPendingAsync();

        var replay = await stream.ReplayAsync(streamId, tenantId);

        Assert.Equal(2, replay.EventCount);
        Assert.Equal(1, replay.AppliedEventCount);
        Assert.Equal(2, replay.Lines.Count);
        Assert.Equal(2500, replay.TotalDebit);
        Assert.Equal(2500, replay.TotalCredit);
        Assert.True(replay.IsBalanced);
        var error = Assert.Single(replay.Errors);
        Assert.Contains("imbalance", error);
    }
}
