using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CO.CostElement;

public class CreateModel : PageModel
{
    private readonly IRepository<CostElementEntity, Guid> _repo;
    public CreateModel(IRepository<CostElementEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public CostElementEntity Element { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Element); return RedirectToPage("/CO/Index"); }
}
