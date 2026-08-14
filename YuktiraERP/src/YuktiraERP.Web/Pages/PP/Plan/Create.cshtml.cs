using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.Plan;
public class CreateModel : PageModel
{
    private readonly IRepository<ProductionPlanEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public CreateModel(IRepository<ProductionPlanEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    [BindProperty] public ProductionPlanEntity Plan { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); Plan.TenantId = _tenant.TenantId; await _repo.AddAsync(Plan); return RedirectToPage("/PP/Plan/List"); }
}
