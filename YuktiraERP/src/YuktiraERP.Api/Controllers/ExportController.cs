using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService) => _exportService = exportService;

    [HttpPost("grid")]
    public async Task<IActionResult> ExportGrid([FromBody] ExportRequest request)
    {
        var bytes = await _exportService.ExportGridAsync(request.Data, request.Format, request.FileName);
        var contentType = request.Format switch
        {
            ExportFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ExportFormat.Csv => "text/csv",
            ExportFormat.Pdf => "application/pdf",
            ExportFormat.Html => "text/html",
            _ => "text/plain"
        };
        var ext = request.Format.ToString().ToLower();
        return File(bytes, contentType, $"export.{ext}");
    }

    [HttpPost("document")]
    public async Task<IActionResult> GenerateDocument([FromBody] DocumentExportRequest request)
    {
        var bytes = await _exportService.GenerateDocumentAsync(request.TemplateCode, request.Data, request.Format);
        return File(bytes, "application/pdf", $"{request.TemplateCode}.pdf");
    }
}

public class ExportRequest
{
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
    public string? FileName { get; set; }
}

public class DocumentExportRequest
{
    public string TemplateCode { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public string Format { get; set; } = "PDF";
}
