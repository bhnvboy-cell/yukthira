using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.InvoiceVerification;
public class CreateModel : PageModel
{
    private readonly IRepository<InvoiceVerificationEntity, Guid> _repo;
    public CreateModel(IRepository<InvoiceVerificationEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InvoiceVerificationEntity Invoice { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Invoice); return RedirectToPage("/MM/Index"); }
}
