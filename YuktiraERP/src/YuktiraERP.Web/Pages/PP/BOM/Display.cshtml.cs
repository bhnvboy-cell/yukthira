using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.BOM;
public class DisplayModel : PageModel
{
    private readonly IRepository<BillOfMaterialEntity, Guid> _repo;
    public DisplayModel(IRepository<BillOfMaterialEntity, Guid> repo) { _repo = repo; }
    public BillOfMaterialEntity Bom { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PP/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Bom = entity;
        return Page();
    }
}
