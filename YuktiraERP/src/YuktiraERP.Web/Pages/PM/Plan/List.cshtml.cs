using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PM.Plan;

[Authorize]
public class ListModel : PageModel
{
    private readonly IRepository<MaintenancePlanEntity, Guid> _repo;
    public ListModel(IRepository<MaintenancePlanEntity, Guid> repo) { _repo = repo; }
    public List<MaintenancePlanEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.GetAllAsync();

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
