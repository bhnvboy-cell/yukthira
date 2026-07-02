using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.ProductionOrder;
public class EditModel : PageModel
{
    private readonly IRepository<ProductionOrderEntity, Guid> _repo;
    public EditModel(IRepository<ProductionOrderEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ProductionOrderEntity ProdOrder { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PP/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        ProdOrder = entity;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(ProdOrder);
        return RedirectToPage("/PP/Index");
    }
}
