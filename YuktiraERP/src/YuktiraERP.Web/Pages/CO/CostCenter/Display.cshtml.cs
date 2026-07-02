using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CO.CostCenter;
public class DisplayModel : PageModel
{
    private readonly IRepository<CostCenterEntity, Guid> _repo;
    public DisplayModel(IRepository<CostCenterEntity, Guid> repo) { _repo = repo; }
    public CostCenterEntity CostCenter { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/CO/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        CostCenter = entity;
        return Page();
    }
}
