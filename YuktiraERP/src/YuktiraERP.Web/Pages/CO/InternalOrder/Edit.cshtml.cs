using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CO.InternalOrder;

public class EditModel : PageModel
{
    private readonly IRepository<InternalOrderEntity, Guid> _repo;
    public EditModel(IRepository<InternalOrderEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InternalOrderEntity Order { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CO/InternalOrder/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Order = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Order);
        return RedirectToPage("/CO/InternalOrder/List");
    }
}
