using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.SD.Quotation;
public class CreateModel : PageModel
{
    private readonly IRepository<QuotationEntity, Guid> _repo;
    public CreateModel(IRepository<QuotationEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public QuotationEntity Quote { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Quote); return RedirectToPage("/SD/Index"); }
}
