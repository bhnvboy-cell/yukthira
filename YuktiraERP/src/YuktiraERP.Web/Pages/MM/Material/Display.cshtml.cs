using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.Material;
public class DisplayModel : PageModel
{
    private readonly IRepository<MaterialMasterEntity, Guid> _repo;
    public DisplayModel(IRepository<MaterialMasterEntity, Guid> repo) { _repo = repo; }
    public MaterialMasterEntity Material { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Material = entity;
        return Page();
    }
}
