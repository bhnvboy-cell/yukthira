using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR.Leave;
public class CreateModel : PageModel
{
    private readonly IRepository<LeaveRequestEntity, Guid> _repo;
    public CreateModel(IRepository<LeaveRequestEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public LeaveRequestEntity LeaveRequest { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(LeaveRequest); return RedirectToPage("/HR/Index"); }
}
