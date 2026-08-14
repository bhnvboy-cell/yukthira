using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.ProductionOrder;
public class EditModel : PageModel
{
    private readonly IRepository<ProductionOrderEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public EditModel(IRepository<ProductionOrderEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    [BindProperty] public ProductionOrderEntity ProdOrder { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PP/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null || entity.TenantId != _tenant.TenantId) return NotFound();
        ProdOrder = entity;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        ProdOrder.TenantId = _tenant.TenantId;
        await _repo.UpdateAsync(ProdOrder);
        return RedirectToPage("/PP/Index");
    }
}
