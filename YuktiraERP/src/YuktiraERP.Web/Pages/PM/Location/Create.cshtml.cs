using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PM.Location;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<FunctionalLocationEntity, Guid> _repo;
    public CreateModel(IRepository<FunctionalLocationEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public FunctionalLocationEntity Location { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Location); return RedirectToPage("/PM/Index"); }
}
