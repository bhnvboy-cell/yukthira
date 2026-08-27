using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Infrastructure.Caching;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.SerialNumber;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IBatchService _batchService;

    public DisplayModel(IBatchService batchService) => _batchService = batchService;

    public SerialNumberEntity Serial { get; set; } = new();
    public List<SerialNumberEntity> SerialHistory { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/SerialNumber/List");
        var entity = await _batchService.GetSerialNumberAsync(id.Value);
        if (entity == null) return NotFound();
        Serial = entity;
        SerialHistory = await _batchService.GetSerialHistoryAsync(entity.SerialNumber);
        return Page();
    }
}
