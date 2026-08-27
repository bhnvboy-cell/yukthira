using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Infrastructure.Caching;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.Batch;

[Authorize]
public class TraceabilityModel : PageModel
{
    private readonly IBatchService _batchService;

    public TraceabilityModel(IBatchService batchService) => _batchService = batchService;

    public BatchTraceabilityResult TraceResult { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/Batch/List");
        TraceResult = await _batchService.GetBatchTraceabilityAsync(id.Value);
        if (TraceResult.Batch == null) return NotFound();
        return Page();
    }
}
