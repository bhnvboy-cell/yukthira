using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.QM.UsageDecision;

public class EditModel : PageModel
{
    private readonly IRepository<UsageDecisionEntity, Guid> _repo;
    public EditModel(IRepository<UsageDecisionEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public UsageDecisionEntity Decision { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/QM/UsageDecision/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Decision = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Decision);
        return RedirectToPage("/QM/UsageDecision/List");
    }
}
