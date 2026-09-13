using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Admin;

[Authorize(Policy = "AdminOrAbove")]
public class DataSyncModel : PageModel
{
    private readonly IModuleDataSyncService _syncService;
    private readonly ISyncSessionStore _sessionStore;

    public DataSyncModel(IModuleDataSyncService syncService, ISyncSessionStore sessionStore)
    {
        _syncService = syncService;
        _sessionStore = sessionStore;
    }

    public List<ModuleListItemDto> Modules { get; set; } = new();
    public ModuleEntityMetaDto? Metadata { get; set; }
    public ImportValidationResultDto? ValidationResult { get; set; }
    public DataSyncExecuteResultDto? SyncResult { get; set; }
    public string? SelectedModule { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }
    public int CurrentStep { get; set; } = 1;

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    [BindProperty]
    public string? ModuleSelection { get; set; }

    [BindProperty]
    public string? SessionToken { get; set; }

    [BindProperty]
    public bool SkipErrors { get; set; }

    public async Task OnGetAsync()
    {
        Modules = await _syncService.GetAvailableModulesAsync();
    }

    public async Task<IActionResult> OnPostSelectModuleAsync()
    {
        Modules = await _syncService.GetAvailableModulesAsync();
        if (!string.IsNullOrEmpty(ModuleSelection) && Enum.TryParse<DataSyncModule>(ModuleSelection, out var module))
        {
            SelectedModule = ModuleSelection;
            Metadata = await _syncService.GetEntityMetadataAsync(module);
            CurrentStep = 1;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostDownloadTemplateAsync()
    {
        Modules = await _syncService.GetAvailableModulesAsync();
        if (!string.IsNullOrEmpty(ModuleSelection) && Enum.TryParse<DataSyncModule>(ModuleSelection, out var module))
        {
            SelectedModule = ModuleSelection;
            var tenantId = GetTenantId();
            var bytes = await _syncService.GenerateTemplateAsync(module, tenantId);
            var fileName = $"{module}_Template_{DateTime.UtcNow:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        Message = "Please select a module first.";
        IsError = true;
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        Modules = await _syncService.GetAvailableModulesAsync();
        SelectedModule = ModuleSelection;

        DataSyncModule? module = null;
        if (!string.IsNullOrEmpty(ModuleSelection) && Enum.TryParse<DataSyncModule>(ModuleSelection, out var parsed))
        {
            module = parsed;
            Metadata = await _syncService.GetEntityMetadataAsync(parsed);
        }

        if (UploadedFile == null || UploadedFile.Length == 0)
        {
            Message = "No file selected. Please choose an .xlsx file.";
            IsError = true;
            CurrentStep = 2;
            return Page();
        }

        if (!UploadedFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            Message = "Only .xlsx files are supported.";
            IsError = true;
            CurrentStep = 2;
            return Page();
        }

        try
        {
            var tenantId = GetTenantId();
            var rows = ParseExcelFile(UploadedFile);
            var result = await _syncService.ValidateUploadAsync(module.Value, rows, tenantId);

            var token = await _sessionStore.CreateSessionAsync(new ModuleSyncSessionDto
            {
                Module = module.Value,
                FileName = UploadedFile.FileName,
                CreatedAt = DateTime.UtcNow,
                ValidationResult = result,
                Rows = rows
            });

            result.SessionToken = token;
            ValidationResult = result;
            CurrentStep = 3;

            if (result.IsValid)
                Message = $"Validation passed. {result.ValidRows} rows ready for sync.";
            else
            {
                Message = $"Validation found {result.ErrorRows} error(s) in {result.TotalRows} rows. Review errors below.";
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            Message = $"Upload failed: {ex.Message}";
            IsError = true;
            CurrentStep = 2;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSyncAsync()
    {
        Modules = await _syncService.GetAvailableModulesAsync();
        if (!string.IsNullOrEmpty(ModuleSelection) && Enum.TryParse<DataSyncModule>(ModuleSelection, out var module))
        {
            SelectedModule = ModuleSelection;
            Metadata = await _syncService.GetEntityMetadataAsync(module);
        }

        if (string.IsNullOrEmpty(SessionToken))
        {
            Message = "Session expired. Please re-upload the file.";
            IsError = true;
            CurrentStep = 2;
            return Page();
        }

        try
        {
            var tenantId = GetTenantId();
            var userId = GetUserId();
            var session = await _sessionStore.GetSessionAsync(SessionToken);

            if (session == null)
            {
                Message = "Session expired. Please re-upload the file.";
                IsError = true;
                CurrentStep = 2;
                return Page();
            }

            var result = await _syncService.ExecuteSyncAsync(
                session.Module, session.Rows, SkipErrors, tenantId, userId);

            SyncResult = result;
            CurrentStep = 3;

            if (result.Success)
                Message = result.Message;
            else
            {
                Message = $"Sync failed: {result.Message}";
                IsError = true;
            }

            await _sessionStore.RemoveSessionAsync(SessionToken);
            SessionToken = null;
        }
        catch (Exception ex)
        {
            Message = $"Sync error: {ex.Message}";
            IsError = true;
            CurrentStep = 3;
        }

        return Page();
    }

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

    private Guid GetUserId() =>
        Guid.TryParse(System.Security.Claims.ClaimTypes.NameIdentifier switch { _ => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value }, out var uid) ? uid : Guid.Empty;

    private static List<Dictionary<string, string>> ParseExcelFile(IFormFile file)
    {
        var rows = new List<Dictionary<string, string>>();
        using var stream = file.OpenReadStream();
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);

        var dataSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name != "Instructions" && ws.Name != "Schema");

        if (dataSheet == null && workbook.Worksheets.Count > 0)
            dataSheet = workbook.Worksheets.Last();

        if (dataSheet == null) return rows;

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
