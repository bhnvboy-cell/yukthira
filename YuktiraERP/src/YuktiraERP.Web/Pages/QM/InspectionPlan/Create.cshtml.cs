using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.QM.InspectionPlan;
public class CreateModel : PageModel
{
    private readonly IRepository<InspectionPlanEntity, Guid> _repo;
    public CreateModel(IRepository<InspectionPlanEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InspectionPlanEntity Plan { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Plan); return RedirectToPage("/QM/Index"); }
}
