using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/telemetry")]
public class MesTelemetryController : MesControllerBase
{
    private readonly MesDataCollectionService _service;

    public MesTelemetryController(MesDataCollectionService service) => _service = service;

    public record TagSampleDto(Guid TagId, DateTime Ts, double? ValueNum, string? ValueText, TagQuality Quality, long Seq, Guid MessageUuid);
    public record IngestBatchRequest(string SourceId, List<TagSampleDto> Samples);

    [HttpPost("tags/batch")]
    public async Task<IActionResult> IngestBatch([FromBody] IngestBatchRequest req, CancellationToken ct)
    {
        var entities = req.Samples.Select(s => new MesTagValueRaw
        {
            TagId = s.TagId,
            Ts = s.Ts,
            ValueNum = s.ValueNum,
            ValueText = s.ValueText,
            Quality = s.Quality,
            Seq = s.Seq,
            MessageUuid = s.MessageUuid
        });
        var (inserted, deduplicated) = await _service.IngestTagBatchAsync(
            CurrentTenantId, CurrentPlantCode, req.SourceId, entities, ct);
        return Ok(new { inserted, deduplicated });
    }

    public record ScanRequest(string TerminalId, string RawCode, string CodeType, Guid MessageUuid);

    [HttpPost("scans")]
    public async Task<IActionResult> RecordScan([FromBody] ScanRequest req, CancellationToken ct)
    {
        var scan = await _service.RecordScanAsync(
            CurrentTenantId, CurrentPlantCode, req.TerminalId, null, req.RawCode, req.CodeType, req.MessageUuid, 2, ct);
        return Ok(scan);
    }
}
