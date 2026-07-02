using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.BI;
public class DashboardDisplayModel : PageModel
{
    private readonly IRepository<DashboardEntity, Guid> _repo;
    public DashboardDisplayModel(IRepository<DashboardEntity, Guid> repo) { _repo = repo; }
    public List<DashboardEntity> Dashboards { get; set; } = new();
    public async Task OnGetAsync() { Dashboards = await _repo.GetAllAsync(); }
}
