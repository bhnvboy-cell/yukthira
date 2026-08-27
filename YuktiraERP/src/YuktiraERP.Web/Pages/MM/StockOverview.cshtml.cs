using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM;
[Authorize]
public class StockOverviewModel : PageModel
{
    private readonly IRepository<StockItemEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public StockOverviewModel(IRepository<StockItemEntity, Guid> repo, ITenantContext tenant) { _repo = repo; _tenant = tenant; }
    public List<StockItemEntity> StockItems { get; set; } = new();
    public async Task OnGetAsync() { StockItems = await _repo.FindAsync(s => s.TenantId == _tenant.TenantId); }
}
