using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.QM.InspectionResult;

public class EditModel : PageModel
{
    private readonly IRepository<InspectionResultEntity, Guid> _repo;
    public EditModel(IRepository<InspectionResultEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InspectionResultEntity Result { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/QM/InspectionResult/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Result = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Result);
        return RedirectToPage("/QM/InspectionResult/List");
    }
}
