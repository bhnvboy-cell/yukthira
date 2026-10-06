using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Messaging;

public class FinancialEventStream : IFinancialEventStream, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FinancialEventStreamOptions _options;
    private readonly ILogger<FinancialEventStream>? _logger;
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _streamGates = new();

    public FinancialEventStream(
        IServiceScopeFactory scopeFactory,
        IOptions<FinancialEventStreamOptions> options,
        ILogger<FinancialEventStream>? logger = null)
    {
        _scopeFactory = scopeFactory;
        _options = options?.Value ?? new FinancialEventStreamOptions();
        _logger = logger;
    }

    public int ChannelDepth => _queue.Reader.Count;

    public async Task<FinancialEventEnvelope> EnqueueAsync(FinancialEventInput input, CancellationToken ct = default)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        var streamId = input.StreamId == Guid.Empty ? Guid.NewGuid() : input.StreamId;
        var gate = _streamGates.GetOrAdd(streamId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();

            var last = await db.FinancialEvents.AsNoTracking()
                .Where(e => e.StreamId == streamId)
                .OrderByDescending(e => e.Sequence)
                .FirstOrDefaultAsync(ct);

            var sequence = (last?.Sequence ?? 0) + 1;
            var previousHash = last == null || string.IsNullOrEmpty(last.Hash)
                ? FinancialEventStreamOptions.GenesisHash
                : last.Hash;
            var eventType = input.EventType ?? "";
            var payload = input.Payload ?? "{}";
            var hash = ComputeHash(sequence, eventType, payload, previousHash);

            var entity = new FinancialEventEntity
            {
                Id = Guid.NewGuid(),
                TenantId = input.TenantId,
                StreamId = streamId,
                StreamType = string.IsNullOrWhiteSpace(input.StreamType) ? "UniversalJournal" : input.StreamType,
                Sequence = sequence,
                EventType = eventType,
                Payload = payload,
                CorrelationId = input.CorrelationId ?? "",
                Status = "Pending",
                PreviousHash = previousHash,
                Hash = hash,
                CreatedAt = DateTime.UtcNow
            };

            db.FinancialEvents.Add(entity);
            await db.SaveChangesAsync(ct);

            _queue.Writer.TryWrite(entity.Id);

            return new FinancialEventEnvelope
            {
                EventId = entity.Id,
                TenantId = entity.TenantId,
                StreamId = entity.StreamId,
                StreamType = entity.StreamType,
                Sequence = entity.Sequence,
                EventType = entity.EventType,
                Status = entity.Status,
                PreviousHash = entity.PreviousHash,
                Hash = entity.Hash,
                CreatedAt = entity.CreatedAt,
                CorrelationId = entity.CorrelationId
            };
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<int> DrainPendingAsync(CancellationToken ct = default)
    {
        while (_queue.Reader.TryRead(out _))
        {
        }

        var attempted = new HashSet<Guid>();
        var batchSize = Math.Max(1, _options.BatchSize);
        var processed = 0;

        while (processed < batchSize)
        {
            ct.ThrowIfCancellationRequested();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();
            var journal = scope.ServiceProvider.GetRequiredService<IUniversalJournalService>();

            var remaining = batchSize - processed;
            var pending = await db.FinancialEvents
                .Where(e => e.Status == "Pending")
                .OrderBy(e => e.StreamId)
                .ThenBy(e => e.Sequence)
                .Take(remaining + attempted.Count)
                .ToListAsync(ct);

            var batch = pending.Where(e => !attempted.Contains(e.Id)).Take(remaining).ToList();
            if (batch.Count == 0) break;

            foreach (var entity in batch)
            {
                attempted.Add(entity.Id);
                await ApplyAsync(db, journal, entity, ct);
                processed++;
            }
        }

        return processed;
    }

    public async Task<IReadOnlyList<FinancialEventDto>> GetStreamAsync(Guid streamId, Guid tenantId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();

        var events = await db.FinancialEvents.AsNoTracking()
            .Where(e => e.StreamId == streamId && e.TenantId == tenantId)
            .OrderBy(e => e.Sequence)
            .ToListAsync();

        return events.Select(e => new FinancialEventDto
        {
            Id = e.Id,
            TenantId = e.TenantId,
            StreamId = e.StreamId,
            StreamType = e.StreamType,
            Sequence = e.Sequence,
            EventType = e.EventType,
            Payload = e.Payload,
            CorrelationId = e.CorrelationId,
            Status = e.Status,
            PreviousHash = e.PreviousHash,
            Hash = e.Hash,
            AppliedAt = e.AppliedAt,
            Error = e.Error,
            CreatedAt = e.CreatedAt
        }).ToList();
    }

    public async Task<StreamReplayResult> ReplayAsync(Guid streamId, Guid tenantId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();

        var events = await db.FinancialEvents.AsNoTracking()
            .Where(e => e.StreamId == streamId && e.TenantId == tenantId)
            .OrderBy(e => e.Sequence)
            .ToListAsync(ct);

        var result = new StreamReplayResult
        {
            StreamId = streamId,
            EventCount = events.Count
        };

        foreach (var entity in events)
        {
            if (entity.Status == "Failed")
            {
                result.Errors.Add($"Sequence {entity.Sequence}: {entity.Error}");
                continue;
            }

            if (entity.Status == "Applied") result.AppliedEventCount++;

            try
            {
                if (entity.EventType == FinancialEventStreamOptions.JournalPostedEvent)
                {
                    var request = DeserializePayload<JournalPostRequest>(entity.Payload);
                    foreach (var line in request.LineItems)
                    {
                        result.Lines.Add(ToReplayLine(request, line));
                    }
                }
                else if (entity.EventType == FinancialEventStreamOptions.JournalBatchPostedEvent)
                {
                    var request = DeserializePayload<JournalBatchPostRequest>(entity.Payload);
                    foreach (var journal in request.Journals)
                    {
                        foreach (var line in journal.LineItems)
                        {
                            result.Lines.Add(ToReplayLine(journal, line));
                        }
                    }
                }
                else
                {
                    result.Errors.Add($"Sequence {entity.Sequence}: unsupported event type {entity.EventType}");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Sequence {entity.Sequence}: {ex.Message}");
            }
        }

        result.TotalDebit = result.Lines.Sum(l => l.Debit);
        result.TotalCredit = result.Lines.Sum(l => l.Credit);
        result.IsBalanced = Math.Abs(result.TotalDebit - result.TotalCredit) <= 0.01m;
        return result;
    }

    private async Task ApplyAsync(
        YuktiraDbContext db,
        IUniversalJournalService journal,
        FinancialEventEntity entity,
        CancellationToken ct)
    {
        try
        {
            if (entity.EventType == FinancialEventStreamOptions.JournalPostedEvent)
            {
                var request = DeserializePayload<JournalPostRequest>(entity.Payload);
                var postResult = await journal.PostAsync(request);
                if (postResult.Success)
                {
                    await MarkAppliedAsync(db, entity, ct);
                }
                else
                {
                    await MarkFailedAsync(db, entity, postResult.Message, ct);
                }
            }
            else if (entity.EventType == FinancialEventStreamOptions.JournalBatchPostedEvent)
            {
                var request = DeserializePayload<JournalBatchPostRequest>(entity.Payload);
                var batchResult = await journal.PostBatchAsync(request);
                if (batchResult.AllSucceeded)
                {
                    await MarkAppliedAsync(db, entity, ct);
                }
                else
                {
                    var error = batchResult.Errors.Count > 0
                        ? string.Join(" | ", batchResult.Errors.Select(e => e.ErrorMessage))
                        : "Batch posting failed";
                    await MarkFailedAsync(db, entity, error, ct);
                }
            }
            else
            {
                await MarkFailedAsync(db, entity, $"Unsupported event type: {entity.EventType}", ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Failed to apply financial event {EventId}", entity.Id);
            await MarkFailedAsync(db, entity, ex.Message, ct);
        }
    }

    private static async Task MarkAppliedAsync(YuktiraDbContext db, FinancialEventEntity entity, CancellationToken ct)
    {
        entity.Status = "Applied";
        entity.AppliedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Error = "";
        await db.SaveChangesAsync(ct);
    }

    private async Task MarkFailedAsync(YuktiraDbContext db, FinancialEventEntity entity, string error, CancellationToken ct)
    {
        try
        {
            entity.Status = "Failed";
            entity.Error = error ?? "";
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Failed to mark financial event {EventId} as failed", entity.Id);
        }
    }

    private static T DeserializePayload<T>(string payload) where T : class
    {
        if (string.IsNullOrWhiteSpace(payload)) throw new InvalidOperationException("Payload is empty");
        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Payload could not be parsed");
    }

    private static StreamReplayLine ToReplayLine(JournalPostRequest request, JournalLineItemRequest line)
    {
        return new StreamReplayLine
        {
            AccountCode = line.GlAccount,
            DocumentType = request.DocumentType,
            Currency = line.Currency,
            Description = line.Description,
            Reference = request.Reference,
            Debit = line.DebitAmount,
            Credit = line.CreditAmount
        };
    }

    private static string ComputeHash(long sequence, string eventType, string payload, string previousHash)
    {
        var raw = $"{sequence}|{eventType}|{payload}|{previousHash}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    public void Dispose()
    {
        try
        {
            _queue.Writer.TryComplete();
        }
        catch
        {
        }

        foreach (var gate in _streamGates.Values)
        {
            gate.Dispose();
        }
        _streamGates.Clear();
    }
}
