using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.Attendance;

[Authorize]
public class EditModel : PageModel
{
    private readonly IRepository<AttendanceEntity, Guid> _repo;
    public EditModel(IRepository<AttendanceEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public AttendanceEntity Attendance { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/HR/Attendance/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Attendance = entity;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.UpdateAsync(Attendance);
        return RedirectToPage("/HR/Attendance/List");
    }
}
