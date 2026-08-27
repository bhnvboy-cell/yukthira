using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CO.ProfitCenter;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<ProfitCenterEntity, Guid> _repo;
    public EditModel(IRepository<ProfitCenterEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ProfitCenterEntity Center { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CO/ProfitCenter/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Center = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Center);
        return RedirectToPage("/CO/ProfitCenter/List");
    }
}
