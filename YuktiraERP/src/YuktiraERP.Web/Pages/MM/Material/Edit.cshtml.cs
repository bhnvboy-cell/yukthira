using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.Material;
public class EditModel : PageModel
{
    private readonly IRepository<MaterialMasterEntity, Guid> _repo;
    public EditModel(IRepository<MaterialMasterEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public MaterialMasterEntity Material { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Material = entity;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Material);
        return RedirectToPage("/MM/Index");
    }
}
