using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.QM.InspectionPlan;
public class EditModel : PageModel
{
    private readonly IRepository<InspectionPlanEntity, Guid> _repo;
    public EditModel(IRepository<InspectionPlanEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InspectionPlanEntity Plan { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/QM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Plan = entity;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Plan);
        return RedirectToPage("/QM/Index");
    }
}
