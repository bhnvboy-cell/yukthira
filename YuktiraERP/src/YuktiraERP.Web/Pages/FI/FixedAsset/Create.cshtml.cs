using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI.FixedAsset;
public class CreateModel : PageModel
{
    private readonly IRepository<FixedAssetEntity, Guid> _repo;
    public CreateModel(IRepository<FixedAssetEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public FixedAssetEntity Asset { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Asset); return RedirectToPage("/FI/Index"); }
}
