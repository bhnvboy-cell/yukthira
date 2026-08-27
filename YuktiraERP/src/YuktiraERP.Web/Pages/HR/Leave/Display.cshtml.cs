using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.Leave;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<LeaveRequestEntity, Guid> _repo;
    public DisplayModel(IRepository<LeaveRequestEntity, Guid> repo) { _repo = repo; }
    public LeaveRequestEntity LeaveRequest { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/HR/Leave/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        LeaveRequest = entity;
        return Page();
    }
}
