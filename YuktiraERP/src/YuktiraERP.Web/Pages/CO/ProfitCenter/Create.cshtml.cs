using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CO.ProfitCenter;

public class CreateModel : PageModel
{
    private readonly IRepository<ProfitCenterEntity, Guid> _repo;
    public CreateModel(IRepository<ProfitCenterEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ProfitCenterEntity Center { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Center); return RedirectToPage("/CO/Index"); }
}
