using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP.Plan;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<ProductionPlanEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public EditModel(IRepository<ProductionPlanEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    [BindProperty] public ProductionPlanEntity Plan { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PP/Plan/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null || entity.TenantId != _tenant.TenantId) return NotFound();
        Plan = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Plan.TenantId = _tenant.TenantId;
        await _repo.UpdateAsync(Plan);
        return RedirectToPage("/PP/Plan/List");
    }
}
