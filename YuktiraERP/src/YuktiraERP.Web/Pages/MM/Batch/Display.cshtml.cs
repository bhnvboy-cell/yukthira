using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Caching;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.Batch;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<BatchEntity, Guid> _batchRepo;
    private readonly IBatchService _batchService;

    public DisplayModel(IRepository<BatchEntity, Guid> batchRepo, IBatchService batchService)
    {
        _batchRepo = batchRepo;
        _batchService = batchService;
    }

    public BatchEntity Batch { get; set; } = new();
    public List<BatchMovementEntity> Movements { get; set; } = new();
    public List<SerialNumberEntity> SerialNumbers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/Batch/List");
        var entity = await _batchRepo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Batch = entity;
        Movements = await _batchService.GetBatchHistoryAsync(id.Value);
        SerialNumbers = await _batchService.GetSerialNumbersByBatchAsync(id.Value);
        return Page();
    }
}
