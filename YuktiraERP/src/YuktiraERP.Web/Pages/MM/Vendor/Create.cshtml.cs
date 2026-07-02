using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.Vendor;
public class CreateModel : PageModel
{
    private readonly IRepository<VendorEntity, Guid> _repo;
    public CreateModel(IRepository<VendorEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public VendorEntity Vendor { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Vendor); return RedirectToPage("/MM/Index"); }
}
