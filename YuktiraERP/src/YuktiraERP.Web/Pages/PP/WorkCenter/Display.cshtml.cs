using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP.WorkCenter;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<WorkCenterEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public DisplayModel(IRepository<WorkCenterEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    public WorkCenterEntity WorkCenter { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PP/WorkCenter/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null || entity.TenantId != _tenant.TenantId) return NotFound();
        WorkCenter = entity;
        return Page();
    }
}
