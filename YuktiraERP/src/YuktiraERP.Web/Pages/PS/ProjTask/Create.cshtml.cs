using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PS.ProjTask;

public class CreateModel : PageModel
{
    private readonly IRepository<ProjectTaskEntity, Guid> _repo;
    public CreateModel(IRepository<ProjectTaskEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ProjectTaskEntity ProjTask { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(ProjTask); return RedirectToPage("/PS/Index"); }
}
