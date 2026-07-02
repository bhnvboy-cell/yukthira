using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP;

public class IndexModel : PageModel
{
    private readonly IRepository<ProductionPlanEntity, Guid> _planRepo;
    private readonly IRepository<WorkCenterEntity, Guid> _wcRepo;
    private readonly IRepository<ProductionOrderEntity, Guid> _poRepo;

    public List<ProductionPlanEntity> ProductionPlans { get; set; } = new();
    public List<WorkCenterEntity> WorkCenters { get; set; } = new();
    public List<ProductionOrderEntity> ProductionOrders { get; set; } = new();

    public IndexModel(
        IRepository<ProductionPlanEntity, Guid> planRepo,
        IRepository<WorkCenterEntity, Guid> wcRepo,
        IRepository<ProductionOrderEntity, Guid> poRepo)
    {
        _planRepo = planRepo;
        _wcRepo = wcRepo;
        _poRepo = poRepo;
    }

    public async Task OnGetAsync()
    {
        ProductionPlans = await _planRepo.GetAllAsync();
        WorkCenters = await _wcRepo.GetAllAsync();
        ProductionOrders = await _poRepo.GetAllAsync();
    }
}
