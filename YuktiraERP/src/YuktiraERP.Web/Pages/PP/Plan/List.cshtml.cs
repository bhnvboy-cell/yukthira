using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP.Plan;

[Authorize]
public class ListModel : PageModel
{
    private readonly IRepository<ProductionPlanEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public ListModel(IRepository<ProductionPlanEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    public List<ProductionPlanEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.FindAsync(x => x.TenantId == _tenant.TenantId);

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null || item.TenantId != _tenant.TenantId) return NotFound();
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
