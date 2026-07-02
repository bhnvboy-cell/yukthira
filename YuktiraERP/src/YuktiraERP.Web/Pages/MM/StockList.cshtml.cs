using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM;
public class StockListModel : PageModel
{
    private readonly IRepository<StockItemEntity, Guid> _repo;
    public StockListModel(IRepository<StockItemEntity, Guid> repo) { _repo = repo; }
    public List<StockItemEntity> StockItems { get; set; } = new();
    public async Task OnGetAsync() { StockItems = await _repo.GetAllAsync(); }
}
