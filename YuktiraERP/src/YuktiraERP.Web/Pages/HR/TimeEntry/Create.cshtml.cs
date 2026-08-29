using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.TimeEntry;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<TimeEntryEntity, Guid> _repo;
    private readonly ITenantContext _tenant;

    public CreateModel(IRepository<TimeEntryEntity, Guid> repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    [BindProperty]
    public TimeEntryEntity TimeEntry { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        TimeEntry.EntryId = "TE-" + DateTime.Now.Ticks;
        TimeEntry.TenantId = _tenant.TenantId;
        if (TimeEntry.EntryDate == default)
            TimeEntry.EntryDate = DateTime.UtcNow;
        await _repo.AddAsync(TimeEntry);
        return RedirectToPage("/HR/Index");
    }
}
