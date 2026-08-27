using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CO.CostElement;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<CostElementEntity, Guid> _repo;
    public EditModel(IRepository<CostElementEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public CostElementEntity Element { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CO/CostElement/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Element = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Element);
        return RedirectToPage("/CO/CostElement/List");
    }
}
