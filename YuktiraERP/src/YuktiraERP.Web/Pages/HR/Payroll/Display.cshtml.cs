using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.Payroll;

public class DisplayModel : PageModel
{
    private readonly IRepository<PayrollEntryEntity, Guid> _repo;
    public DisplayModel(IRepository<PayrollEntryEntity, Guid> repo) { _repo = repo; }
    public PayrollEntryEntity Entry { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/HR/Payroll/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Entry = entity;
        return Page();
    }
}
