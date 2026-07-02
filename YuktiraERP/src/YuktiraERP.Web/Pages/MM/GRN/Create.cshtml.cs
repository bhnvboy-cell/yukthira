using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.GRN;
public class CreateModel : PageModel
{
    private readonly IRepository<GoodsReceiptEntity, Guid> _repo;
    public CreateModel(IRepository<GoodsReceiptEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public GoodsReceiptEntity Receipt { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Receipt); return RedirectToPage("/MM/Index"); }
}
