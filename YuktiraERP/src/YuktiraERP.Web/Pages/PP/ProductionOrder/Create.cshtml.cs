using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP.ProductionOrder;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<ProductionOrderEntity, Guid> _repo;
    private readonly IRepository<BillOfMaterialEntity, Guid> _bomRepo;
    private readonly IRepository<ProductionRoutingEntity, Guid> _routingRepo;
    private readonly ITenantContext _tenant;

    public CreateModel(
        IRepository<ProductionOrderEntity, Guid> repo,
        IRepository<BillOfMaterialEntity, Guid> bomRepo,
        IRepository<ProductionRoutingEntity, Guid> routingRepo,
        ITenantContext tenant)
    {
        _repo = repo;
        _bomRepo = bomRepo;
        _routingRepo = routingRepo;
        _tenant = tenant;
    }

    [BindProperty] public ProductionOrderEntity Order { get; set; } = new();
    public SelectList BOMOptions { get; set; } = new(Enumerable.Empty<BillOfMaterialEntity>(), "Id", "ProductName");
    public SelectList RoutingOptions { get; set; } = new(Enumerable.Empty<ProductionRoutingEntity>(), "Id", "ProductName");

    public async Task<IActionResult> OnGetAsync()
    {
        var boms = await _bomRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
        var routings = await _routingRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
        BOMOptions = new SelectList(boms, "Id", "ProductName");
        RoutingOptions = new SelectList(routings, "Id", "ProductName");
        Order.StartDate = DateTime.UtcNow;
        Order.EndDate = DateTime.UtcNow.AddDays(7);
        Order.Plant = "1000";
        Order.BaseUOM = "EA";
        Order.OrderType = "PP01";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        Order.TenantId = _tenant.TenantId;
        Order.OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        await _repo.AddAsync(Order);
        return RedirectToPage("/PP/Index");
    }
}
