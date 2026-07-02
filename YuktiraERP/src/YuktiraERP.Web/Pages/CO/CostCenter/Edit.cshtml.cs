using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CO.CostCenter;
public class EditModel : PageModel
{
    private readonly IRepository<CostCenterEntity, Guid> _repo;
    public EditModel(IRepository<CostCenterEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public CostCenterEntity CostCenter { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CO/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        CostCenter = entity;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(CostCenter);
        return RedirectToPage("/CO/Index");
    }
}
