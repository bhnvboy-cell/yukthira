using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.BOM;
public class EditModel : PageModel
{
    private readonly IRepository<BillOfMaterialEntity, Guid> _repo;
    public EditModel(IRepository<BillOfMaterialEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public BillOfMaterialEntity Bom { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PP/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Bom = entity;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Bom);
        return RedirectToPage("/PP/Index");
    }
}
