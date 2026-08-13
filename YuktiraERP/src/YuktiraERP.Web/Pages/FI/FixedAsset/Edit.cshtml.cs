using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.FI.FixedAsset;

public class EditModel : PageModel
{
    private readonly IRepository<FixedAssetEntity, Guid> _repo;
    public EditModel(IRepository<FixedAssetEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public FixedAssetEntity Asset { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/FI/FixedAsset/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Asset = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Asset);
        return RedirectToPage("/FI/FixedAsset/List");
    }
}
