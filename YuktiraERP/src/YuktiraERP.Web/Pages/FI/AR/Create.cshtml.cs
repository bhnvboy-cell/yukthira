using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI.AR;
public class CreateModel : PageModel
{
    private readonly IRepository<AREntryEntity, Guid> _repo;
    public CreateModel(IRepository<AREntryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public AREntryEntity Entry { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Entry); return RedirectToPage("/FI/Index"); }
}
