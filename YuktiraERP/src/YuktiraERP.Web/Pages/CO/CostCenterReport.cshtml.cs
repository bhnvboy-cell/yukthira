using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CO;
public class CostCenterReportModel : PageModel
{
    private readonly IRepository<CostCenterEntity, Guid> _repo;
    public CostCenterReportModel(IRepository<CostCenterEntity, Guid> repo) { _repo = repo; }
    public List<CostCenterEntity> CostCenters { get; set; } = new();
    public async Task OnGetAsync() { CostCenters = await _repo.GetAllAsync(); }
}
