using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.PO;
public class EditModel : PageModel
{
    private readonly IRepository<PurchaseOrderEntity, Guid> _repo;
    public EditModel(IRepository<PurchaseOrderEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public PurchaseOrderEntity Order { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Order = entity;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Order);
        return RedirectToPage("/MM/Index");
    }
}
