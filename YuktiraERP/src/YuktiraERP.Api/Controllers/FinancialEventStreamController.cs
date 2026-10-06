using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/fi/event-stream")]
[Authorize]
public class FinancialEventStreamController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IFinancialEventStream _stream;
    private readonly IColumnarJournalCache _cache;
    private readonly IDuckdbAnalyticsService _analytics;
    private readonly FinancialEventStreamOptions _options;

    public FinancialEventStreamController(
        YuktiraDbContext db,
        ITenantContext tenant,
        IFinancialEventStream stream,
        IColumnarJournalCache cache,
        IDuckdbAnalyticsService analytics,
        IOptions<FinancialEventStreamOptions> options)
    {
        _db = db;
        _tenant = tenant;
        _stream = stream;
        _cache = cache;
        _analytics = analytics;
        _options = options?.Value ?? new FinancialEventStreamOptions();
    }

    [HttpPost("journal-event")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> JournalEvent([FromBody] FinancialEventInput input, CancellationToken ct)
    {
        input ??= new FinancialEventInput();
        if (input.TenantId == Guid.Empty) input.TenantId = _tenant.TenantId;

        var envelope = await _stream.EnqueueAsync(input, ct);
        return Ok(new
        {
            success = true,
            eventId = envelope.EventId,
            tenantId = envelope.TenantId,
            streamId = envelope.StreamId,
            streamType = envelope.StreamType,
            sequence = envelope.Sequence,
            eventType = envelope.EventType,
            status = envelope.Status,
            previousHash = envelope.PreviousHash,
            hash = envelope.Hash,
            createdAt = envelope.CreatedAt,
            correlationId = envelope.CorrelationId
        });
    }

    [HttpGet("outbox")]
    public async Task<IActionResult> Outbox(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = _tenant.TenantId;
        var query = _db.FinancialEvents.AsNoTracking().Where(e => e.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);

        var total = await query.CountAsync(ct);
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : Math.Min(pageSize, 500);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Sequence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.StreamId,
                e.StreamType,
                e.Sequence,
                e.EventType,
                e.Status,
                e.CorrelationId,
                e.PreviousHash,
                e.Hash,
                e.AppliedAt,
                e.Error,
                e.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("stream/{streamId:guid}")]
    public async Task<IActionResult> GetStream(Guid streamId, CancellationToken ct)
    {
        var events = await _stream.GetStreamAsync(streamId, _tenant.TenantId);
        if (events.Count == 0) return NotFound(new { message = "No financial events found for this stream" });

        return Ok(new
        {
            streamId,
            tenantId = _tenant.TenantId,
            eventCount = events.Count,
            events
        });
    }

    [HttpPost("stream/{streamId:guid}/replay")]
    public async Task<IActionResult> Replay(Guid streamId, CancellationToken ct)
    {
        var replay = await _stream.ReplayAsync(streamId, _tenant.TenantId, ct);
        if (replay.EventCount == 0) return NotFound(new { message = "No financial events found for this stream" });

        return Ok(new
        {
            streamId = replay.StreamId,
            eventCount = replay.EventCount,
            appliedEventCount = replay.AppliedEventCount,
            lineCount = replay.Lines.Count,
            totalDebit = replay.TotalDebit,
            totalCredit = replay.TotalCredit,
            isBalanced = replay.IsBalanced,
            errors = replay.Errors,
            lines = replay.Lines
        });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct)
    {
        var tenantId = _tenant.TenantId;
        var events = _db.FinancialEvents.AsNoTracking().Where(e => e.TenantId == tenantId);

        var total = await events.CountAsync(ct);
        var pending = await events.CountAsync(e => e.Status == "Pending", ct);
        var applied = await events.CountAsync(e => e.Status == "Applied", ct);
        var failed = await events.CountAsync(e => e.Status == "Failed", ct);

        return Ok(new
        {
            enabled = _options.Enabled,
            channelDepth = _stream.ChannelDepth,
            flushMilliseconds = _options.FlushMilliseconds,
            batchSize = _options.BatchSize,
            tenantId,
            totalEvents = total,
            pending,
            applied,
            failed,
            cacheRowCount = _cache.RowCount,
            cacheLastBuiltAt = _cache.LastBuiltAt
        });
    }

    [HttpPost("columnar/query")]
    public async Task<IActionResult> ColumnarQuery([FromBody] ColumnarQuery query, CancellationToken ct)
    {
        query ??= new ColumnarQuery();
        query.TenantId ??= _tenant.TenantId;

        var result = await _analytics.QueryAsync(query, ct);
        return Ok(new
        {
            engine = result.Engine,
            usedFallback = result.UsedFallback,
            error = result.Error,
            columns = result.Columns,
            matchedRows = result.MatchedRows,
            elapsedMilliseconds = result.ElapsedMilliseconds,
            rows = result.Rows
        });
    }

    [HttpPost("parquet/query")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> ParquetQuery([FromBody] ColumnarQuery query, CancellationToken ct)
    {
        query ??= new ColumnarQuery();
        query.TenantId = null;
        query.Source = "parquet";

        var result = await _analytics.QueryAsync(query, ct);
        return Ok(new
        {
            engine = result.Engine,
            usedFallback = result.UsedFallback,
            error = result.Error,
            columns = result.Columns,
            matchedRows = result.MatchedRows,
            elapsedMilliseconds = result.ElapsedMilliseconds,
            rows = result.Rows
        });
    }

    [HttpPost("columnar/export")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> ColumnarExport([FromQuery] int? fiscalYear = null, CancellationToken ct = default)
    {
        try
        {
            var path = await _cache.ExportAsync(fiscalYear, ct);
            return Ok(new
            {
                success = true,
                fiscalYear = fiscalYear ?? DateTime.UtcNow.Year,
                path,
                rowCount = _cache.RowCount
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("columnar/status")]
    public async Task<IActionResult> ColumnarStatus(CancellationToken ct)
    {
        var status = await _cache.GetStatusAsync(ct);
        return Ok(new
        {
            enabled = status.Enabled,
            lastExportAt = status.LastExportAt,
            lastError = status.LastError,
            totalRows = status.TotalRows,
            rowCount = _cache.RowCount,
            lastBuiltAt = _cache.LastBuiltAt,
            files = status.Files
        });
    }
}
