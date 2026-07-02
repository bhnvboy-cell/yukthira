using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.SD.Billing;
public class CreateModel : PageModel
{
    private readonly IRepository<BillingDocumentEntity, Guid> _repo;
    public CreateModel(IRepository<BillingDocumentEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public BillingDocumentEntity Invoice { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Invoice); return RedirectToPage("/SD/Index"); }
}
