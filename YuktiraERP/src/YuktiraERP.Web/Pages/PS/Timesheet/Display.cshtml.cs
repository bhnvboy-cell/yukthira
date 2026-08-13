using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PS.Timesheet;

public class DisplayModel : PageModel
{
    private readonly IRepository<TimesheetEntryEntity, Guid> _repo;
    public DisplayModel(IRepository<TimesheetEntryEntity, Guid> repo) { _repo = repo; }
    public TimesheetEntryEntity Entry { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/PS/Timesheet/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Entry = entity;
        return Page();
    }
}
