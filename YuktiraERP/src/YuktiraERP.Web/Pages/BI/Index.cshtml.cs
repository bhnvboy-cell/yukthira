using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.BI;

public class IndexModel : PageModel
{
    private readonly IRepository<DashboardEntity, Guid> _dashboardRepo;

    public List<DashboardEntity> Dashboards { get; set; } = new();

    public IndexModel(IRepository<DashboardEntity, Guid> dashboardRepo)
    {
        _dashboardRepo = dashboardRepo;
    }

    public async Task OnGetAsync()
    {
        Dashboards = await _dashboardRepo.GetAllAsync();
    }
}
