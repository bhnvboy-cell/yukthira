using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesDataCollectionService
{
    private readonly MesDbContext _db;

    public MesDataCollectionService(MesDbContext db) => _db = db;

    /// <summary>
    /// Idempotent batch ingestion of tag values using (TenantId, SourceId, MessageUuid).
    /// </summary>
    public async Task<(int Inserted, int Deduplicated)> IngestTagBatchAsync(
        Guid tenantId,
        string plantCode,
        string sourceId,
        IEnumerable<MesTagValueRaw> values,
        CancellationToken ct = default)
    {
        var list = values.ToList();
        if (list.Count == 0) return (0, 0);

        var uuids = list.Select(v => v.MessageUuid).Distinct().ToList();
        var existingUuids = await _db.TagValuesRaw
            .Where(t => t.TenantId == tenantId && t.SourceId == sourceId && uuids.Contains(t.MessageUuid))
            .Select(t => t.MessageUuid)
            .ToHashSetAsync(ct);

        int inserted = 0, deduplicated = 0;
        foreach (var item in list)
        {
            if (existingUuids.Contains(item.MessageUuid))
            {
                deduplicated++;
                continue;
            }
            item.TenantId = tenantId;
            item.PlantCode = plantCode;
            item.SourceId = sourceId;
            _db.TagValuesRaw.Add(item);
            existingUuids.Add(item.MessageUuid);
            inserted++;
        }

        await _db.SaveChangesAsync(ct);
        return (inserted, deduplicated);
    }

    /// <summary>
    /// Records a barcode/RFID/QR scan with duplicate suppression within duplicateWindowSeconds (default 2s).
    /// </summary>
    public async Task<MesScanEvent> RecordScanAsync(
        Guid tenantId,
        string plantCode,
        string terminalId,
        Guid? userId,
        string rawCode,
        string codeType,
        Guid messageUuid,
        int duplicateWindowSeconds = 2,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            var rejected = new MesScanEvent
            {
                TenantId = tenantId,
                PlantCode = plantCode,
                TerminalId = terminalId,
                UserId = userId,
                RawCode = rawCode ?? "",
                CodeType = codeType,
                Result = ScanResult.REJECTED,
                RejectReason = "Empty or invalid barcode",
                MessageUuid = messageUuid,
                Ts = now
            };
            _db.ScanEvents.Add(rejected);
            await _db.SaveChangesAsync(ct);
            return rejected;
        }

        var windowStart = now.AddSeconds(-duplicateWindowSeconds);
        var isDuplicate = await _db.ScanEvents.AnyAsync(s =>
            s.TenantId == tenantId &&
            s.TerminalId == terminalId &&
            s.RawCode == rawCode &&
            s.Ts >= windowStart &&
            s.Result == ScanResult.ACCEPTED, ct);

        var scan = new MesScanEvent
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            TerminalId = terminalId,
            UserId = userId,
            RawCode = rawCode,
            CodeType = codeType,
            Result = isDuplicate ? ScanResult.DUPLICATE : ScanResult.ACCEPTED,
            RejectReason = isDuplicate ? $"Duplicate scan within {duplicateWindowSeconds}s" : null,
            MessageUuid = messageUuid,
            Ts = now
        };
        _db.ScanEvents.Add(scan);
        await _db.SaveChangesAsync(ct);
        return scan;
    }

    public async Task SendToDeadLetterAsync(
        Guid tenantId, string plantCode, string source, Guid messageUuid, object payload, string error, CancellationToken ct = default)
    {
        _db.DeadLetters.Add(new MesDeadLetter
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            Source = source,
            MessageUuid = messageUuid,
            PayloadJson = JsonSerializer.Serialize(payload),
            Error = error.Length > 500 ? error[..500] : error,
            ReceivedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
