using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.Routing;
public class CreateModel : PageModel
{
    private readonly IRepository<ProductionRoutingEntity, Guid> _repo;
    public CreateModel(IRepository<ProductionRoutingEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ProductionRoutingEntity Routing { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Routing); return RedirectToPage("/PP/Index"); }
}
