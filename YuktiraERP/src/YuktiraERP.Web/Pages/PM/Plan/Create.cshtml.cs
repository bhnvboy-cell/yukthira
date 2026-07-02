using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PM.Plan;

public class CreateModel : PageModel
{
    private readonly IRepository<MaintenancePlanEntity, Guid> _repo;
    public CreateModel(IRepository<MaintenancePlanEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public MaintenancePlanEntity Plan { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Plan); return RedirectToPage("/PM/Index"); }
}
