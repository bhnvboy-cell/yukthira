using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PS.Project;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<ProjectEntity, Guid> _repo;
    public DisplayModel(IRepository<ProjectEntity, Guid> repo) { _repo = repo; }
    public ProjectEntity Project { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        Project = item;
        return Page();
    }
}
