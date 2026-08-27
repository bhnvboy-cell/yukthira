using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PM.Order;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<MaintenanceOrderEntity, Guid> _repo;
    public EditModel(IRepository<MaintenanceOrderEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public MaintenanceOrderEntity Order { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PM/Order/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Order = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Order);
        return RedirectToPage("/PM/Order/List");
    }
}
