using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PS.Timesheet;

public class CreateModel : PageModel
{
    private readonly IRepository<TimesheetEntryEntity, Guid> _repo;
    public CreateModel(IRepository<TimesheetEntryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public TimesheetEntryEntity Entry { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Entry); return RedirectToPage("/PS/Index"); }
}
