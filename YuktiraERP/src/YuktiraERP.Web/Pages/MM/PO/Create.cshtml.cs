using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.PO;
public class CreateModel : PageModel
{
    private readonly IRepository<PurchaseOrderEntity, Guid> _repo;
    public CreateModel(IRepository<PurchaseOrderEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public PurchaseOrderEntity Order { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Order); return RedirectToPage("/MM/Index"); }
}
