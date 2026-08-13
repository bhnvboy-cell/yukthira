using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.Payroll;

public class EditModel : PageModel
{
    private readonly IRepository<PayrollEntryEntity, Guid> _repo;
    public EditModel(IRepository<PayrollEntryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public PayrollEntryEntity Entry { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/HR/Payroll/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Entry = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Entry);
        return RedirectToPage("/HR/Payroll/List");
    }
}
