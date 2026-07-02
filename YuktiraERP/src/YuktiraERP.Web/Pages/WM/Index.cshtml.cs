using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.WM;

public class IndexModel : PageModel
{
    private readonly IRepository<StockItemEntity, Guid> _stockRepo;
    private readonly IRepository<StorageLocationEntity, Guid> _locationRepo;

    public List<StockItemEntity> StockItems { get; set; } = new();
    public List<StorageLocationEntity> StorageLocations { get; set; } = new();

    public IndexModel(
        IRepository<StockItemEntity, Guid> stockRepo,
        IRepository<StorageLocationEntity, Guid> locationRepo)
    {
        _stockRepo = stockRepo;
        _locationRepo = locationRepo;
    }

    public async Task OnGetAsync()
    {
        StockItems = await _stockRepo.GetAllAsync();
        StorageLocations = await _locationRepo.GetAllAsync();
    }
}
