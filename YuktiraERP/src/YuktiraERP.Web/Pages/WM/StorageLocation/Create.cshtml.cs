using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.WM.StorageLocation;
public class CreateModel : PageModel
{
    private readonly IRepository<StorageLocationEntity, Guid> _repo;
    public CreateModel(IRepository<StorageLocationEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public StorageLocationEntity Location { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Location); return RedirectToPage("/WM/Index"); }
}
