using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/data-sync")]
[Authorize]
public class DataSyncController : ControllerBase
{
    private readonly IModuleDataSyncService _syncService;
    private readonly ISyncSessionStore _sessionStore;
    private readonly ILogger<DataSyncController> _logger;

    public DataSyncController(
        IModuleDataSyncService syncService,
        ISyncSessionStore sessionStore,
        ILogger<DataSyncController> logger)
    {
        _syncService = syncService;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    private Guid GetUserId() =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

    [HttpGet("modules")]
    public async Task<IActionResult> GetAvailableModules()
    {
        var modules = await _syncService.GetAvailableModulesAsync();
        return Ok(modules);
    }

    [HttpGet("metadata/{module}")]
    public async Task<IActionResult> GetModuleMetadata([FromRoute] DataSyncModule module)
    {
        try
        {
            var meta = await _syncService.GetEntityMetadataAsync(module);
            return Ok(meta);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("template/{module}")]
    public async Task<IActionResult> DownloadTemplate([FromRoute] DataSyncModule module)
    {
        try
        {
            var tenantId = GetTenantId();
            var bytes = await _syncService.GenerateTemplateAsync(module, tenantId);
            var fileName = $"{module}_Template_{DateTime.UtcNow:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("upload/{module}")]
    public async Task<IActionResult> UploadAndValidate(
        [FromRoute] DataSyncModule module,
        [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only .xlsx files are supported" });

        try
        {
            var tenantId = GetTenantId();
            var rows = ParseExcelFile(file);
            var result = await _syncService.ValidateUploadAsync(module, rows, tenantId);

            var sessionToken = await _sessionStore.CreateSessionAsync(new ModuleSyncSessionDto
            {
                Module = module,
                FileName = file.FileName,
                CreatedAt = DateTime.UtcNow,
                ValidationResult = result,
                Rows = rows
            });

            result.SessionToken = sessionToken;
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload validation failed for module {Module}", module);
            return StatusCode(500, new { error = $"Upload failed: {ex.Message}" });
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> ExecuteSync([FromBody] DataSyncExecuteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionToken))
            return BadRequest(new { error = "Session token is required" });

        var session = await _sessionStore.GetSessionAsync(request.SessionToken);
        if (session == null)
            return BadRequest(new { error = "Session expired or not found. Please re-upload the file." });

        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();

            var result = await _syncService.ExecuteSyncAsync(
                request.Module,
                session.Rows,
                request.SkipErrors,
                tenantId,
                userId);

            await _sessionStore.RemoveSessionAsync(request.SessionToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync execution failed for module {Module}", request.Module);
            return StatusCode(500, new { error = $"Sync failed: {ex.Message}" });
        }
    }

    private static List<Dictionary<string, string>> ParseExcelFile(IFormFile file)
    {
        var rows = new List<Dictionary<string, string>>();

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var dataSheet = workbook.Worksheets.FirstOrDefault(worksheet =>
            worksheet.Name != "Instructions" && worksheet.Name != "Schema"
        );

        if (dataSheet == null && workbook.Worksheets.Count > 0)
            dataSheet = workbook.Worksheets.Last();

        if (dataSheet == null)
            return rows;

        var headerRow = dataSheet.Row(1);
        var headers = new List<string>();
        int colCount = 0;

        while (!headerRow.Cell(colCount + 1).IsEmpty())
        {
            headers.Add(headerRow.Cell(colCount + 1).GetString().Trim());
            colCount++;
        }

        if (colCount == 0) return rows;

        var lastRow = dataSheet.LastRowUsed();
        if (lastRow == null) return rows;

        int totalRows = lastRow.RowNumber();

        for (int r = 2; r <= totalRows; r++)
        {
            var row = dataSheet.Row(r);
            if (row.CellsUsed().All(c => c.IsEmpty())) continue;

            var dict = new Dictionary<string, string>();
            for (int c = 0; c < headers.Count; c++)
            {
                var cell = row.Cell(c + 1);
                var value = cell.GetString()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(value))
                    dict[headers[c]] = value;
            }

            if (dict.Count > 0)
                rows.Add(dict);
        }

        return rows;
    }
}
