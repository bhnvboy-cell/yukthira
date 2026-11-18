using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Services;

namespace YuktiraERP.Mes.Plugin.Controllers;

[Route("api/v1/mes/documents")]
public class MesDocumentsController : MesControllerBase
{
    private readonly MesDocumentService _service;

    public MesDocumentsController(MesDocumentService service) => _service = service;

    public record CreateRevRequest(string DocNo, MesDocType DocType, string Title, Guid OwnerId, string Revision, string FileHash, string StorageUri);

    [HttpPost]
    public async Task<IActionResult> CreateRevision([FromBody] CreateRevRequest req, CancellationToken ct)
    {
        var (doc, rev) = await _service.CreateDocumentRevisionAsync(
            CurrentTenantId, CurrentPlantCode, req.DocNo, req.DocType, req.Title, req.OwnerId, req.Revision, req.FileHash, req.StorageUri, ct);
        return Ok(new { doc, rev });
    }

    [HttpPost("revisions/{revId:guid}/effective")]
    public async Task<IActionResult> MakeEffective(Guid revId, CancellationToken ct)
        => Ok(await _service.MakeEffectiveAsync(CurrentTenantId, revId, ct));

    public record SignRequest(Guid UserId, ESignatureMeaning Meaning, string AuthMethod = "PASSWORD_MFA");

    [HttpPost("esignatures")]
    public async Task<IActionResult> Sign([FromBody] SignRequest req, CancellationToken ct)
        => Ok(await _service.SignAsync(CurrentTenantId, CurrentPlantCode, req.UserId, req.Meaning, req.AuthMethod, ct));

    public record EbrStepRequest(int StepNo, string Value, string Uom, decimal? LowLimit, decimal? HighLimit, Guid? EsignId);

    [HttpPost("ebr/{ebrId:guid}/steps")]
    public async Task<IActionResult> RecordEbrStep(Guid ebrId, [FromBody] EbrStepRequest req, CancellationToken ct)
        => Ok(await _service.RecordEbrStepAsync(
            CurrentTenantId, CurrentPlantCode, ebrId, req.StepNo, req.Value, req.Uom, req.LowLimit, req.HighLimit, CurrentActor, req.EsignId, ct));
}
