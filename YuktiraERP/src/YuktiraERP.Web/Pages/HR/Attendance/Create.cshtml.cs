using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR.Attendance;
public class CreateModel : PageModel
{
    private readonly IRepository<AttendanceEntity, Guid> _repo;
    public CreateModel(IRepository<AttendanceEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public AttendanceEntity Attendance { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Attendance); return RedirectToPage("/HR/Index"); }
}
