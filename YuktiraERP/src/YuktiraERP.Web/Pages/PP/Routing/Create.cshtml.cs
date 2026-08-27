using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.Routing;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<ProductionRoutingEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public CreateModel(IRepository<ProductionRoutingEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    [BindProperty] public ProductionRoutingEntity Routing { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); Routing.TenantId = _tenant.TenantId; await _repo.AddAsync(Routing); return RedirectToPage("/PP/Routing/List"); }
}
