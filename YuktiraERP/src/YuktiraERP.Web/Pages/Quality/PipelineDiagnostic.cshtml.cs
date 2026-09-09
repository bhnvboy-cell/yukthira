using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Pages.Quality;

[Authorize]
public class PipelineDiagnosticModel : PageModel
{
    private readonly IPipelineDiagnosticService _diagnosticService;
    private readonly ITenantContext _tenant;

    public PipelineDiagnosticModel(IPipelineDiagnosticService diagnosticService, ITenantContext tenant)
    {
        _diagnosticService = diagnosticService;
        _tenant = tenant;
    }

    [BindProperty]
    public PipelineDiagnosticRequest Request { get; set; } = new();

    public PipelineDiagnosticResult? Result { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostExecuteAsync()
    {
        try
        {
            var userId = User.Identity?.Name ?? "SYSTEM";
            Result = await _diagnosticService.ExecuteFullPipelineDiagnosticAsync(Request, _tenant.TenantId, userId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Pipeline diagnostic failed: {ex.Message}";
        }
        return Page();
    }
}
