using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PS.ProjTask;

public class EditModel : PageModel
{
    private readonly IRepository<ProjectTaskEntity, Guid> _repo;
    public EditModel(IRepository<ProjectTaskEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ProjectTaskEntity ProjTask { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PS/ProjTask/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        ProjTask = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(ProjTask);
        return RedirectToPage("/PS/ProjTask/List");
    }
}
