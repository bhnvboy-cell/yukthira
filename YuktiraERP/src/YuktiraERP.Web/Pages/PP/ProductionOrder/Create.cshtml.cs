using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.ProductionOrder;
public class CreateModel : PageModel
{
    private readonly IRepository<ProductionOrderEntity, Guid> _repo;
    public CreateModel(IRepository<ProductionOrderEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ProductionOrderEntity Order { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Order); return RedirectToPage("/PP/Index"); }
}
