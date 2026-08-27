using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.LIMS.Specification;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<SpecificationEntity, Guid> _repo;
    public DisplayModel(IRepository<SpecificationEntity, Guid> repo) { _repo = repo; }
    public SpecificationEntity Spec { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/LIMS/Specification/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Spec = entity;
        return Page();
    }
}
