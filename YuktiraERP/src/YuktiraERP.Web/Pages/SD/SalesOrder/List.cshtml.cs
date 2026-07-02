using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.SD.SalesOrder;
public class ListModel : PageModel
{
    private readonly IRepository<SalesOrderEntity, Guid> _repo;
    public ListModel(IRepository<SalesOrderEntity, Guid> repo) { _repo = repo; }
    public List<SalesOrderEntity> Orders { get; set; } = new();
    public async Task OnGetAsync() { Orders = await _repo.GetAllAsync(); }
}
