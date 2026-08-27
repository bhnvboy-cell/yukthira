using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.MM.InvoiceVerification;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<InvoiceVerificationEntity, Guid> _repo;
    private readonly YuktiraDbContext _db;
    public CreateModel(IRepository<InvoiceVerificationEntity, Guid> repo, YuktiraDbContext db) { _repo = repo; _db = db; }
    [BindProperty] public InvoiceVerificationEntity Invoice { get; set; } = new();

    public IActionResult OnGet()
    {
        Invoice.Date = DateTime.UtcNow;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var tenantId = _db.TenantId ?? Guid.Empty;
        Invoice.TenantId = tenantId;

        if (!string.IsNullOrEmpty(Invoice.PoNumber))
        {
            var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.PoNumber == Invoice.PoNumber);
            if (po != null)
            {
                var poItems = await _db.PurchaseOrderItems.Where(i => i.PurchaseOrderId == po.Id).ToListAsync();
                Invoice.MatchedAmount = poItems.Sum(i => i.TotalPrice);

                var variance = Invoice.MatchedAmount != 0
                    ? Math.Abs(Invoice.Amount - Invoice.MatchedAmount) / Invoice.MatchedAmount * 100
                    : 0;

                var priceTolerance = decimal.TryParse(Request.Form["PriceTolerance"].FirstOrDefault() ?? "5", out var pt) ? pt : 5m;
                Invoice.Status = variance <= priceTolerance ? "Matched" : "Variance";
            }
        }

        await _repo.AddAsync(Invoice);
        return RedirectToPage("/MM/InvoiceVerification/List");
    }
}
