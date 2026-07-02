using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.BI.Dashboard;
public class CreateModel : PageModel
{
    private readonly IRepository<DashboardEntity, Guid> _repo;
    public CreateModel(IRepository<DashboardEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public DashboardEntity Dashboard { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Dashboard); return RedirectToPage("/BI/Index"); }
}
