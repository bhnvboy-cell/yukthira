using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP.ProductionOrder;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<ProductionOrderEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public CreateModel(IRepository<ProductionOrderEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    [BindProperty] public ProductionOrderEntity Order { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); Order.TenantId = _tenant.TenantId; await _repo.AddAsync(Order); return RedirectToPage("/PP/Index"); }
}
