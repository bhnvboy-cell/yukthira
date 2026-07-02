using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.QM;

public class IndexModel : PageModel
{
    private readonly IRepository<InspectionLotEntity, Guid> _lotRepo;
    private readonly IRepository<InspectionResultEntity, Guid> _resultRepo;
    private readonly IRepository<UsageDecisionEntity, Guid> _decisionRepo;

    public List<InspectionLotEntity> InspectionLots { get; set; } = new();
    public List<InspectionResultEntity> InspectionResults { get; set; } = new();
    public List<UsageDecisionEntity> UsageDecisions { get; set; } = new();

    public IndexModel(
        IRepository<InspectionLotEntity, Guid> lotRepo,
        IRepository<InspectionResultEntity, Guid> resultRepo,
        IRepository<UsageDecisionEntity, Guid> decisionRepo)
    {
        _lotRepo = lotRepo;
        _resultRepo = resultRepo;
        _decisionRepo = decisionRepo;
    }

    public async Task OnGetAsync()
    {
        InspectionLots = await _lotRepo.GetAllAsync();
        InspectionResults = await _resultRepo.GetAllAsync();
        UsageDecisions = await _decisionRepo.GetAllAsync();
    }
}
