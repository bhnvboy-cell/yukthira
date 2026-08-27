using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.Batch;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<BatchEntity, Guid> _batchRepo;

    public EditModel(IRepository<BatchEntity, Guid> batchRepo) => _batchRepo = batchRepo;

    [BindProperty] public BatchEntity Batch { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/Batch/List");
        var entity = await _batchRepo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Batch = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _batchRepo.UpdateAsync(Batch);
        return RedirectToPage("/MM/Batch/List");
    }
}
