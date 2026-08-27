using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PS.ProjTask;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<ProjectTaskEntity, Guid> _repo;
    public DisplayModel(IRepository<ProjectTaskEntity, Guid> repo) { _repo = repo; }
    public ProjectTaskEntity ProjTask { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PS/ProjTask/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        ProjTask = entity;
        return Page();
    }
}
