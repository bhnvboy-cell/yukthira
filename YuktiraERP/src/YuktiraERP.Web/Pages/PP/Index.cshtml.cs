using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IRepository<ProductionPlanEntity, Guid> _planRepo;
    private readonly IRepository<WorkCenterEntity, Guid> _wcRepo;
    private readonly IRepository<ProductionOrderEntity, Guid> _poRepo;
    private readonly ITenantContext _tenant;

    public List<ProductionPlanEntity> ProductionPlans { get; set; } = new();
    public List<WorkCenterEntity> WorkCenters { get; set; } = new();
    public List<ProductionOrderEntity> ProductionOrders { get; set; } = new();

    public IndexModel(
        IRepository<ProductionPlanEntity, Guid> planRepo,
        IRepository<WorkCenterEntity, Guid> wcRepo,
        IRepository<ProductionOrderEntity, Guid> poRepo,
        ITenantContext tenant)
    {
        _planRepo = planRepo;
        _wcRepo = wcRepo;
        _poRepo = poRepo;
        _tenant = tenant;
    }

    public async Task OnGetAsync()
    {
        ProductionPlans = await _planRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
        WorkCenters = await _wcRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
        ProductionOrders = await _poRepo.FindAsync(x => x.TenantId == _tenant.TenantId);
    }
}
