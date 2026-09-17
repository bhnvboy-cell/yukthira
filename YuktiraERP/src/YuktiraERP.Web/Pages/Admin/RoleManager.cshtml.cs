using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Admin;

[Authorize(Policy = "AdminOrAbove")]
public class RoleManagerModel : PageModel
{
    private readonly ISecurityImportService _securityService;
    private readonly ITransactionCodeService _tcodeService;

    public RoleManagerModel(ISecurityImportService securityService, ITransactionCodeService tcodeService)
    {
        _securityService = securityService;
        _tcodeService = tcodeService;
    }

    public List<RoleHierarchyDto> RoleHierarchy { get; set; } = new();
    public List<TransactionCodeDto> AllTcodes { get; set; } = new();
    public List<CompositeRoleDto> CompositeRoles { get; set; } = new();
    public int TotalRoles { get; set; }
    public int TotalTcodes { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }

    [BindProperty] public string? FilterModule { get; set; }
    [BindProperty] public string? SearchQuery { get; set; }
    [BindProperty] public string? AssignUserId { get; set; }
    [BindProperty] public string? AssignRoleId { get; set; }
    [BindProperty] public string? ImportData { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostFilterAsync()
    {
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync()
    {
        if (string.IsNullOrEmpty(AssignUserId) || string.IsNullOrEmpty(AssignRoleId))
        {
            Message = "Please select both User ID and Composite Role.";
            IsError = true;
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var result = await _securityService.AssignCompositeRoleToUserAsync(
                new UserRoleAssignRequest { UserId = AssignUserId, CompositeRoleId = AssignRoleId },
                GetTenantId(), GetUserId().ToString());

            Message = result.Success
                ? $"Role assigned successfully. {result.PermissionsGranted} permissions granted."
                : result.Message;
            IsError = !result.Success;
        }
        catch (Exception ex)
        {
            Message = $"Assignment failed: {ex.Message}";
            IsError = true;
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (string.IsNullOrWhiteSpace(ImportData))
        {
            Message = "Please paste role matrix data before importing.";
            IsError = true;
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var lines = ImportData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var startIdx = 0;
            if (lines.Length > 0)
            {
                var firstLine = lines[0].ToLowerInvariant();
                if (firstLine.Contains("composite") || firstLine.Contains("code"))
                    startIdx = 1;
            }

            var masterRows = new List<SecurityImportRowDto>();
            var compositeRows = new List<CompositeRoleImportRowDto>();

            for (var i = startIdx; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var parts = line.Contains('\t') ? line.Split('\t') : line.Split('|');
                if (parts.Length < 6) continue;

                var compCode = parts[0].Trim();
                var compName = parts[1].Trim();
                var module = parts[2].Trim();
                var derivedCode = parts[3].Trim();
                var derivedName = parts[4].Trim();
                var tcode = parts[5].Trim();

                masterRows.Add(new SecurityImportRowDto
                {
                    Module = module,
                    AppsTcodes = tcode,
                    MasterRole = derivedCode,
                    AppDescription = derivedName
                });

                compositeRows.Add(new CompositeRoleImportRowDto
                {
                    Module = module,
                    CompositeRole = compCode,
                    DerivedRole = derivedCode,
                    MasterRole = derivedName
                });
            }

            var request = new RoleMatrixImportRequest
            {
                MasterRoleRows = masterRows,
                CompositeRoleRows = compositeRows
            };

            var result = await _securityService.ImportFullRoleMatrixAsync(request, GetTenantId(), GetUserId());

            Message = result.Success
                ? $"Import complete. {result.MasterRolesCreated} master roles, {result.CompositeRolesCreated} composite roles, {result.DerivedRolesCreated} derived roles, {result.TCodeAssignmentsCreated} T-Code assignments created."
                : $"Import failed: {result.Errors} error(s). {string.Join("; ", result.ErrorDetails.Take(3).Select(e => e.Message))}";
            IsError = !result.Success;
        }
        catch (Exception ex)
        {
            Message = $"Import failed: {ex.Message}";
            IsError = true;
        }

        await LoadDataAsync();
        return Page();
    }

    private async Task LoadDataAsync()
    {
        var tenantId = GetTenantId();

        try
        {
            RoleHierarchy = await _securityService.GetRoleHierarchyAsync(tenantId);
        }
        catch
        {
            RoleHierarchy = new List<RoleHierarchyDto>();
        }

        try
        {
            CompositeRoles = await _securityService.GetCompositeRolesAsync(tenantId, FilterModule);
        }
        catch
        {
            CompositeRoles = new List<CompositeRoleDto>();
        }

        try
        {
            TotalRoles = await _securityService.GetRoleCountAsync(tenantId);
        }
        catch
        {
            TotalRoles = 0;
        }

        try
        {
            TotalTcodes = await _securityService.GetTCodeCountAsync(tenantId);
        }
        catch
        {
            TotalTcodes = 0;
        }

        try
        {
            AllTcodes = await _tcodeService.GetAllAsync(module: FilterModule, search: SearchQuery);
        }
        catch
        {
            AllTcodes = new List<TransactionCodeDto>();
        }

        if (TotalTcodes == 0 && AllTcodes.Any())
            TotalTcodes = AllTcodes.Count;
    }

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

    private Guid GetUserId() =>
        Guid.TryParse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            out var uid) ? uid : Guid.Empty;
}
