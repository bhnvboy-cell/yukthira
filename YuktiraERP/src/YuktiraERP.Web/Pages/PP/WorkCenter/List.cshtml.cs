using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP.WorkCenter;

public class ListModel : PageModel
{
    private readonly IRepository<WorkCenterEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public ListModel(IRepository<WorkCenterEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    public List<WorkCenterEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.FindAsync(x => x.TenantId == _tenant.TenantId);

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null || item.TenantId != _tenant.TenantId) return NotFound();
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
