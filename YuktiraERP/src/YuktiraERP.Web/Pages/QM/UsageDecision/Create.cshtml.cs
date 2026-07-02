using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.QM.UsageDecision;
public class CreateModel : PageModel
{
    private readonly IRepository<UsageDecisionEntity, Guid> _repo;
    public CreateModel(IRepository<UsageDecisionEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public UsageDecisionEntity Decision { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Decision); return RedirectToPage("/QM/Index"); }
}
