using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IRepository<ProductionPlanEntity, Guid> _planRepo;
    private readonly IRepository<BillOfMaterialEntity, Guid> _bomRepo;
    private readonly IRepository<ProductionRoutingEntity, Guid> _routingRepo;
    private readonly IRepository<WorkCenterEntity, Guid> _wcRepo;
    private readonly IRepository<ProductionOrderEntity, Guid> _poRepo;
    private readonly IRepository<OrderConfirmationEntity, Guid> _confRepo;
    private readonly ITenantContext _tenant;

    public List<ProductionPlanEntity> ProductionPlans { get; set; } = new();
    public List<BillOfMaterialEntity> BillOfMaterials { get; set; } = new();
    public List<ProductionRoutingEntity> Routings { get; set; } = new();
    public List<WorkCenterEntity> WorkCenters { get; set; } = new();
    public List<ProductionOrderEntity> ProductionOrders { get; set; } = new();
    public List<OrderConfirmationEntity> Confirmations { get; set; } = new();

    public int ActiveOrderCount { get; set; }
    public string CapacityUtilization { get; set; } = "0";
    public int ConfirmationsTodayCount { get; set; }
    public int MaterialShortageCount { get; set; }
    public int OnTimeRate { get; set; } = 100;

    public IndexModel(
        IRepository<ProductionPlanEntity, Guid> planRepo,
        IRepository<BillOfMaterialEntity, Guid> bomRepo,
        IRepository<ProductionRoutingEntity, Guid> routingRepo,
        IRepository<WorkCenterEntity, Guid> wcRepo,
        IRepository<ProductionOrderEntity, Guid> poRepo,
        IRepository<OrderConfirmationEntity, Guid> confRepo,
        ITenantContext tenant)
    {
        _planRepo = planRepo;
        _bomRepo = bomRepo;
        _routingRepo = routingRepo;
        _wcRepo = wcRepo;
        _poRepo = poRepo;
        _confRepo = confRepo;
        _tenant = tenant;
    }

    public async Task OnGetAsync()
    {
        ProductionPlans = await _planRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
        BillOfMaterials = await _bomRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
        Routings = await _routingRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
        WorkCenters = await _wcRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
        ProductionOrders = await _poRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
        Confirmations = await _confRepo.FindAsync(x => x.TenantId == _tenant.TenantId);

        var today = DateTime.UtcNow.Date;
        ActiveOrderCount = ProductionOrders.Count(o => o.Status is "PLANNED" or "RELEASED" or "IN_PROGRESS");
        ConfirmationsTodayCount = Confirmations.Count(c => c.ConfirmationDate.Date == today);
        MaterialShortageCount = ProductionOrders.Count(o => o.Status == "PLANNED" && o.Quantity > 0);

        if (WorkCenters.Any(w => w.CapacityPerDay > 0))
        {
            var totalCap = WorkCenters.Where(w => w.CapacityPerDay > 0).Sum(w => w.CapacityPerDay);
            var usedCap = ProductionOrders.Where(o => o.Status == "IN_PROGRESS").Sum(o => (double)o.Quantity);
            CapacityUtilization = totalCap > 0 ? Math.Min(100, Math.Round(usedCap / (double)totalCap * 100)).ToString() : "0";
        }

        var completedOrders = ProductionOrders.Where(o => o.Status is "COMPLETED" or "TECO").ToList();
        if (completedOrders.Any())
        {
            var onTime = completedOrders.Count(o => o.ConfirmedAt <= o.EndDate || !o.ConfirmedAt.HasValue);
            OnTimeRate = (int)Math.Round((double)onTime / completedOrders.Count * 100);
        }
    }
}
