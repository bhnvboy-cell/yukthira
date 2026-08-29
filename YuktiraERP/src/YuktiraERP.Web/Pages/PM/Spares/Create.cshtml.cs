using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PM.Spares;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<SparePartEntity, Guid> _repo;
    public CreateModel(IRepository<SparePartEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public SparePartEntity Spare { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Spare); return RedirectToPage("/PM/Index"); }
}
