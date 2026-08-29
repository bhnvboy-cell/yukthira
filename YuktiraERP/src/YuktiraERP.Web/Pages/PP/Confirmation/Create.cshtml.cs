using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP.Confirmation;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<OrderConfirmationEntity, Guid> _repo;
    private readonly IRepository<ProductionOrderEntity, Guid> _poRepo;
    private readonly ITenantContext _tenant;

    public CreateModel(
        IRepository<OrderConfirmationEntity, Guid> repo,
        IRepository<ProductionOrderEntity, Guid> poRepo,
        ITenantContext tenant)
    {
        _repo = repo;
        _poRepo = poRepo;
        _tenant = tenant;
    }

    [BindProperty] public OrderConfirmationEntity Confirmation { get; set; } = new();
    public SelectList OrderOptions { get; set; } = new(Enumerable.Empty<ProductionOrderEntity>(), "OrderNumber", "OrderNumber");

    public async Task<IActionResult> OnGetAsync()
    {
        var orders = await _poRepo.FindAsync(x => x.TenantId == _tenant.TenantId && x.Status != "TECO" && x.Status != "CANCELLED");
        OrderOptions = new SelectList(orders, "OrderNumber", "OrderNumber");
        Confirmation.ConfirmationDate = DateTime.UtcNow;
        Confirmation.OperationNumber = 10;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var orders = await _poRepo.FindAsync(x => x.TenantId == _tenant.TenantId && x.Status != "TECO" && x.Status != "CANCELLED");
            OrderOptions = new SelectList(orders, "OrderNumber", "OrderNumber");
            return Page();
        }
        Confirmation.TenantId = _tenant.TenantId;
        Confirmation.ConfirmationNumber = $"CNF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        await _repo.AddAsync(Confirmation);
        return RedirectToPage("/PP/Index");
    }
}
