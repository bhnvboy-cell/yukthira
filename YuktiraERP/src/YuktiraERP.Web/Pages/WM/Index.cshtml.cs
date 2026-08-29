using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.WM;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IRepository<StockItemEntity, Guid> _stockRepo;
    private readonly IRepository<StorageLocationEntity, Guid> _locationRepo;
    private readonly IRepository<BinEntity, Guid> _binRepo;
    private readonly IRepository<TransferOrderEntity, Guid> _transferOrderRepo;
    private readonly IRepository<StockMovementEntity, Guid> _movementRepo;
    private readonly IRepository<WaveEntity, Guid> _waveRepo;
    private readonly IRepository<InventoryCountEntity, Guid> _countRepo;

    public List<StockItemEntity> StockItems { get; set; } = new();
    public List<StorageLocationEntity> StorageLocations { get; set; } = new();
    public List<BinEntity> Bins { get; set; } = new();
    public List<TransferOrderEntity> TransferOrders { get; set; } = new();
    public List<StockMovementEntity> Movements { get; set; } = new();
    public List<WaveEntity> Waves { get; set; } = new();
    public List<InventoryCountEntity> InventoryCounts { get; set; } = new();

    public IndexModel(
        IRepository<StockItemEntity, Guid> stockRepo,
        IRepository<StorageLocationEntity, Guid> locationRepo,
        IRepository<BinEntity, Guid> binRepo,
        IRepository<TransferOrderEntity, Guid> transferOrderRepo,
        IRepository<StockMovementEntity, Guid> movementRepo,
        IRepository<WaveEntity, Guid> waveRepo,
        IRepository<InventoryCountEntity, Guid> countRepo)
    {
        _stockRepo = stockRepo;
        _locationRepo = locationRepo;
        _binRepo = binRepo;
        _transferOrderRepo = transferOrderRepo;
        _movementRepo = movementRepo;
        _waveRepo = waveRepo;
        _countRepo = countRepo;
    }

    public async Task OnGetAsync()
    {
        StockItems = await _stockRepo.GetAllAsync();
        StorageLocations = await _locationRepo.GetAllAsync();
        Bins = await _binRepo.GetAllAsync();
        TransferOrders = await _transferOrderRepo.GetAllAsync();
        Movements = await _movementRepo.GetAllAsync();
        Waves = await _waveRepo.GetAllAsync();
        InventoryCounts = await _countRepo.GetAllAsync();
    }
}
