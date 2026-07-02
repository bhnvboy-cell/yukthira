using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM;

public class CreateModel : PageModel
{
    private readonly IRepository<MaterialMasterEntity, Guid> _repo;
    public CreateModel(IRepository<MaterialMasterEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public MaterialMasterEntity Material { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Material); return RedirectToPage("/MM/Index"); }
}
