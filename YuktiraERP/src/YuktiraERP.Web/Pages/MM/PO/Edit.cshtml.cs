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
public class EditModel : PageModel
{
    private readonly IRepository<PurchaseOrderEntity, Guid> _repo;
    private readonly YuktiraDbContext _db;
    private readonly IDepartmentKeyService _deptService;
    public EditModel(IRepository<PurchaseOrderEntity, Guid> repo, YuktiraDbContext db, IDepartmentKeyService deptService) { _repo = repo; _db = db; _deptService = deptService; }
    [BindProperty] public PurchaseOrderEntity Order { get; set; } = new();
    [BindProperty] public List<PurchaseOrderItemEntity> LineItems { get; set; } = new();
    public List<SelectListItem> DepartmentKeyOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/MM/Index");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Order = entity;

        LineItems = await _db.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == id.Value)
            .OrderBy(i => i.LineNumber)
            .ToListAsync();

        if (LineItems.Count == 0) LineItems = new List<PurchaseOrderItemEntity> { new() };

        var tenantId = _db.TenantId ?? Guid.Empty;
        var depts = await _deptService.GetAllAsync(tenantId);
        DepartmentKeyOptions = depts.Select(d => new SelectListItem { Value = d.Code, Text = $"{d.Code} - {d.Name}" }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync(Order.Id);
            return Page();
        }

        Order.TotalAmount = LineItems.Where(l => l.MaterialName != "").Sum(l => l.Quantity * l.UnitPrice);
        Order.ItemCount = LineItems.Count(l => l.MaterialName != "");
        await _repo.UpdateAsync(Order);

        var existingItems = await _db.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == Order.Id)
            .ToListAsync();

        foreach (var item in existingItems)
        {
            _db.PurchaseOrderItems.Remove(item);
        }

        int lineNum = 1;
        foreach (var item in LineItems.Where(l => l.MaterialName != ""))
        {
            item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
            item.TenantId = Order.TenantId;
            item.PurchaseOrderId = Order.Id;
            item.LineNumber = lineNum++;
            item.TotalPrice = item.Quantity * item.UnitPrice;
            _db.PurchaseOrderItems.Add(item);
        }
        await _db.SaveChangesAsync();
        return RedirectToPage("/MM/Index");
    }
}
