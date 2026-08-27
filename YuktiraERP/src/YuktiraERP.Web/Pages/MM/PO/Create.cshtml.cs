using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
namespace YuktiraERP.Web.Pages.MM.PO;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<PurchaseOrderEntity, Guid> _repo;
    private readonly IDepartmentKeyService _deptService;
    private readonly INumberRangeService _numberRange;
    private readonly YuktiraDbContext _db;
    public CreateModel(IRepository<PurchaseOrderEntity, Guid> repo, IDepartmentKeyService deptService, INumberRangeService numberRange, YuktiraDbContext db)
    { _repo = repo; _deptService = deptService; _numberRange = numberRange; _db = db; }

    [BindProperty] public PurchaseOrderEntity Order { get; set; } = new();
    [BindProperty] public List<PurchaseOrderItemEntity> LineItems { get; set; } = new() { new PurchaseOrderItemEntity() };
    public List<SelectListItem> DepartmentKeyOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        Order.Date = DateTime.UtcNow;
        var tenantId = _db.TenantId ?? Guid.Empty;
        var depts = await _deptService.GetAllAsync(tenantId);
        DepartmentKeyOptions = depts.Select(d => new SelectListItem { Value = d.Code, Text = $"{d.Code} - {d.Name}" }).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var tenantId = _db.TenantId ?? Guid.Empty;
        Order.TenantId = tenantId;

        if (string.IsNullOrEmpty(Order.PoNumber))
        {
            Order.PoNumber = await _numberRange.GetNextNumberAsync(tenantId, "MM", "PO");
        }

        Order.TotalAmount = LineItems.Where(l => l.MaterialName != "").Sum(l => l.Quantity * l.UnitPrice);
        Order.ItemCount = LineItems.Count(l => l.MaterialName != "");
        await _repo.AddAsync(Order);

        int lineNum = 1;
        foreach (var item in LineItems.Where(l => l.MaterialName != ""))
        {
            item.Id = Guid.NewGuid();
            item.TenantId = tenantId;
            item.PurchaseOrderId = Order.Id;
            item.LineNumber = lineNum++;
            item.TotalPrice = item.Quantity * item.UnitPrice;
            item.Status = "OPEN";
            _db.PurchaseOrderItems.Add(item);
        }
        await _db.SaveChangesAsync();
        return RedirectToPage("/MM/Index");
    }
}
