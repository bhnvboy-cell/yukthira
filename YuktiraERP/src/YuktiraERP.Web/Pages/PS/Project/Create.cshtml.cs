using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PS.Project;

public class CreateModel : PageModel
{
    private readonly IRepository<ProjectEntity, Guid> _repo;
    public CreateModel(IRepository<ProjectEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ProjectEntity Project { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Project); return RedirectToPage("/PS/Index"); }
}
