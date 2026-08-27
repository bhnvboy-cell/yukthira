using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.Appraisal;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<AppraisalEntity, Guid> _repo;
    public DisplayModel(IRepository<AppraisalEntity, Guid> repo) { _repo = repo; }
    public AppraisalEntity Appraisal { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/HR/Appraisal/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Appraisal = entity;
        return Page();
    }
}
