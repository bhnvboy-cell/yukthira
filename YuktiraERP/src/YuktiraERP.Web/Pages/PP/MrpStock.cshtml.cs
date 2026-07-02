using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP;
public class MrpStockModel : PageModel
{
    private readonly IRepository<ProductionPlanEntity, Guid> _repo;
    public MrpStockModel(IRepository<ProductionPlanEntity, Guid> repo) { _repo = repo; }
    public List<ProductionPlanEntity> Plans { get; set; } = new();
    public async Task OnGetAsync() { Plans = await _repo.GetAllAsync(); }
}
