using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP;

[Authorize]
public class MrpRunModel : PageModel
{
    private readonly IMrpService _mrpService;
    private readonly ITenantContext _tenant;

    public MrpRunModel(IMrpService mrpService, ITenantContext tenant)
    {
        _mrpService = mrpService;
        _tenant = tenant;
    }

    [BindProperty] public string ProductName { get; set; } = "";
    [BindProperty] public int HorizonDays { get; set; } = 30;
    [BindProperty] public double ServiceLevel { get; set; } = 0.95;
    public List<MrpSuggestionDto> Suggestions { get; set; } = new();
    public List<MrpExceptionMessageDto> Exceptions { get; set; } = new();
    public string Result { get; set; } = "";
    public long DurationMs { get; set; }
    public int MaterialsProcessed { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var suggestions = await _mrpService.RunMrpAsync(_tenant.TenantId);
            sw.Stop();

            if (!string.IsNullOrWhiteSpace(ProductName))
            {
                suggestions = suggestions.Where(s =>
                    s.MaterialName.Contains(ProductName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            Suggestions = suggestions;
            DurationMs = sw.ElapsedMilliseconds;
            MaterialsProcessed = suggestions.Count;

            var exceptions = await _mrpService.GetExceptionMessagesAsync(_tenant.TenantId);
            Exceptions = exceptions;

            Result = $"MRP run completed: {Suggestions.Count} suggestions generated, {Exceptions.Count} exceptions found, completed in {DurationMs}ms";
        }
        catch (Exception ex)
        {
            Result = $"MRP run failed: {ex.Message}";
        }

        return Page();
    }
}
