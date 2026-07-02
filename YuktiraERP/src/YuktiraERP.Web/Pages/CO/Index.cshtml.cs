using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CO;

public class IndexModel : PageModel
{
    private readonly IRepository<CostCenterEntity, Guid> _ccRepo;
    private readonly IRepository<CostElementEntity, Guid> _ceRepo;
    private readonly IRepository<ProfitCenterEntity, Guid> _pcRepo;
    private readonly IRepository<InternalOrderEntity, Guid> _ioRepo;

    public List<CostCenterEntity> CostCenters { get; set; } = new();
    public List<CostElementEntity> CostElements { get; set; } = new();
    public List<ProfitCenterEntity> ProfitCenters { get; set; } = new();
    public List<InternalOrderEntity> InternalOrders { get; set; } = new();

    public IndexModel(
        IRepository<CostCenterEntity, Guid> ccRepo,
        IRepository<CostElementEntity, Guid> ceRepo,
        IRepository<ProfitCenterEntity, Guid> pcRepo,
        IRepository<InternalOrderEntity, Guid> ioRepo)
    {
        _ccRepo = ccRepo;
        _ceRepo = ceRepo;
        _pcRepo = pcRepo;
        _ioRepo = ioRepo;
    }

    public async Task OnGetAsync()
    {
        CostCenters = await _ccRepo.GetAllAsync();
        CostElements = await _ceRepo.GetAllAsync();
        ProfitCenters = await _pcRepo.GetAllAsync();
        InternalOrders = await _ioRepo.GetAllAsync();
    }
}
