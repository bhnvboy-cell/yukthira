using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.WorkCenter;
public class CreateModel : PageModel
{
    private readonly IRepository<WorkCenterEntity, Guid> _repo;
    public CreateModel(IRepository<WorkCenterEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public WorkCenterEntity WorkCenter { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(WorkCenter); return RedirectToPage("/PP/Index"); }
}
