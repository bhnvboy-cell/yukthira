using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.Plan;
public class CreateModel : PageModel
{
    private readonly IRepository<ProductionPlanEntity, Guid> _repo;
    public CreateModel(IRepository<ProductionPlanEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ProductionPlanEntity Plan { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Plan); return RedirectToPage("/PP/Index"); }
}
