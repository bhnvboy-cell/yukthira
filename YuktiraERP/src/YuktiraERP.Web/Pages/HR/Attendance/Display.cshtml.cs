using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.Attendance;

public class DisplayModel : PageModel
{
    private readonly IRepository<AttendanceEntity, Guid> _repo;
    public DisplayModel(IRepository<AttendanceEntity, Guid> repo) { _repo = repo; }
    public AttendanceEntity Attendance { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/HR/Attendance/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Attendance = entity;
        return Page();
    }
}
