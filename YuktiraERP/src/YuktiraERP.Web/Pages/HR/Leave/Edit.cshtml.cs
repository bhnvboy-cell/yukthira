using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.Leave;

public class EditModel : PageModel
{
    private readonly IRepository<LeaveRequestEntity, Guid> _repo;
    public EditModel(IRepository<LeaveRequestEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public LeaveRequestEntity LeaveRequest { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/HR/Leave/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        LeaveRequest = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(LeaveRequest);
        return RedirectToPage("/HR/Leave/List");
    }
}
