using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.SD;

public class IndexModel : PageModel
{
    private readonly IRepository<CustomerEntity, Guid> _customerRepo;
    private readonly IRepository<InquiryEntity, Guid> _inquiryRepo;
    private readonly IRepository<QuotationEntity, Guid> _quoteRepo;
    private readonly IRepository<SalesOrderEntity, Guid> _soRepo;
    private readonly IRepository<DeliveryEntity, Guid> _deliveryRepo;
    private readonly IRepository<BillingDocumentEntity, Guid> _billingRepo;

    public List<CustomerEntity> Customers { get; set; } = new();
    public List<InquiryEntity> Inquiries { get; set; } = new();
    public List<QuotationEntity> Quotations { get; set; } = new();
    public List<SalesOrderEntity> SalesOrders { get; set; } = new();
    public List<DeliveryEntity> Deliveries { get; set; } = new();
    public List<BillingDocumentEntity> BillingDocuments { get; set; } = new();

    public IndexModel(
        IRepository<CustomerEntity, Guid> customerRepo,
        IRepository<InquiryEntity, Guid> inquiryRepo,
        IRepository<QuotationEntity, Guid> quoteRepo,
        IRepository<SalesOrderEntity, Guid> soRepo,
        IRepository<DeliveryEntity, Guid> deliveryRepo,
        IRepository<BillingDocumentEntity, Guid> billingRepo)
    {
        _customerRepo = customerRepo;
        _inquiryRepo = inquiryRepo;
        _quoteRepo = quoteRepo;
        _soRepo = soRepo;
        _deliveryRepo = deliveryRepo;
        _billingRepo = billingRepo;
    }

    public async Task OnGetAsync()
    {
        Customers = await _customerRepo.GetAllAsync();
        Inquiries = await _inquiryRepo.GetAllAsync();
        Quotations = await _quoteRepo.GetAllAsync();
        SalesOrders = await _soRepo.GetAllAsync();
        Deliveries = await _deliveryRepo.GetAllAsync();
        BillingDocuments = await _billingRepo.GetAllAsync();
    }
}
