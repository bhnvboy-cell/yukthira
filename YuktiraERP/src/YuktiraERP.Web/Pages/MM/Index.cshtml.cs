using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM;

public class IndexModel : PageModel
{
    private readonly IRepository<MaterialMasterEntity, Guid> _materialRepo;
    private readonly IRepository<VendorEntity, Guid> _vendorRepo;
    private readonly IRepository<PurchaseRequisitionEntity, Guid> _prRepo;
    private readonly IRepository<PurchaseOrderEntity, Guid> _poRepo;
    private readonly IRepository<GoodsReceiptEntity, Guid> _grnRepo;
    private readonly IRepository<StockItemEntity, Guid> _stockRepo;
    private readonly IRepository<InvoiceVerificationEntity, Guid> _invRepo;

    public List<MaterialMasterEntity> Materials { get; set; } = new();
    public List<VendorEntity> Vendors { get; set; } = new();
    public List<PurchaseRequisitionEntity> PurchaseRequisitions { get; set; } = new();
    public List<PurchaseOrderEntity> PurchaseOrders { get; set; } = new();
    public List<GoodsReceiptEntity> GoodsReceipts { get; set; } = new();
    public List<StockItemEntity> StockItems { get; set; } = new();
    public List<InvoiceVerificationEntity> InvoiceVerifications { get; set; } = new();

    public IndexModel(
        IRepository<MaterialMasterEntity, Guid> materialRepo,
        IRepository<VendorEntity, Guid> vendorRepo,
        IRepository<PurchaseRequisitionEntity, Guid> prRepo,
        IRepository<PurchaseOrderEntity, Guid> poRepo,
        IRepository<GoodsReceiptEntity, Guid> grnRepo,
        IRepository<StockItemEntity, Guid> stockRepo,
        IRepository<InvoiceVerificationEntity, Guid> invRepo)
    {
        _materialRepo = materialRepo;
        _vendorRepo = vendorRepo;
        _prRepo = prRepo;
        _poRepo = poRepo;
        _grnRepo = grnRepo;
        _stockRepo = stockRepo;
        _invRepo = invRepo;
    }

    public async Task OnGetAsync()
    {
        Materials = await _materialRepo.GetAllAsync();
        Vendors = await _vendorRepo.GetAllAsync();
        PurchaseRequisitions = await _prRepo.GetAllAsync();
        PurchaseOrders = await _poRepo.GetAllAsync();
        GoodsReceipts = await _grnRepo.GetAllAsync();
        StockItems = await _stockRepo.GetAllAsync();
        InvoiceVerifications = await _invRepo.GetAllAsync();
    }
}
