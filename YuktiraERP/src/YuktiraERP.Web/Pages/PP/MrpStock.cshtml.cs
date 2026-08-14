using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.PP;
public class MrpStockModel : PageModel
{
    private readonly IRepository<ProductionPlanEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public MrpStockModel(IRepository<ProductionPlanEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    public List<ProductionPlanEntity> Plans { get; set; } = new();
    public async Task OnGetAsync() { Plans = await _repo.FindAsync(x => x.TenantId == _tenant.TenantId); }
}
