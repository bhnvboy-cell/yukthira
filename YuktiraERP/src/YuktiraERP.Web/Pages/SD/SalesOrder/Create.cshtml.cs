using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
namespace YuktiraERP.Web.Pages.SD.SalesOrder;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<SalesOrderEntity, Guid> _repo;
    private readonly INumberRangeService _numberRange;
    private readonly YuktiraDbContext _db;
    public CreateModel(IRepository<SalesOrderEntity, Guid> repo, INumberRangeService numberRange, YuktiraDbContext db)
    { _repo = repo; _numberRange = numberRange; _db = db; }

    [BindProperty] public SalesOrderEntity Order { get; set; } = new();
    [BindProperty] public List<SalesOrderLineEntity> LineItems { get; set; } = new() { new SalesOrderLineEntity() };

    public async Task OnGetAsync()
    {
        Order.OrderDate = DateTime.UtcNow;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { await OnGetAsync(); return Page(); }

        if (string.IsNullOrEmpty(Order.OrderNumber))
        {
            Order.OrderNumber = await _numberRange.GetNextNumberAsync(Guid.Empty, "SD", "SO");
        }

        Order.Amount = LineItems.Where(l => !string.IsNullOrEmpty(l.MaterialName)).Sum(l => l.Quantity * l.UnitPrice);
        Order.ItemCount = LineItems.Count(l => !string.IsNullOrEmpty(l.MaterialName));
        Order.Status = "Pending";
        await _repo.AddAsync(Order);

        foreach (var item in LineItems.Where(l => !string.IsNullOrEmpty(l.MaterialName)))
        {
            item.Id = Guid.NewGuid();
            item.SalesOrderId = Order.Id;
            item.TotalPrice = item.Quantity * item.UnitPrice;
            _db.SalesOrderLines.Add(item);
        }
        await _db.SaveChangesAsync();
        return RedirectToPage("/SD/Index");
    }
}
