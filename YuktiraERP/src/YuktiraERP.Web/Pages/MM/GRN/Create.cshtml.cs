using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
namespace YuktiraERP.Web.Pages.MM.GRN;
[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<GoodsReceiptEntity, Guid> _repo;
    private readonly YuktiraDbContext _db;
    private readonly INumberRangeService _numberRange;
    public CreateModel(IRepository<GoodsReceiptEntity, Guid> repo, YuktiraDbContext db, INumberRangeService numberRange) { _repo = repo; _db = db; _numberRange = numberRange; }
    [BindProperty] public GoodsReceiptEntity Receipt { get; set; } = new();
    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var tenantId = _db.TenantId ?? Guid.Empty;
        Receipt.TenantId = tenantId;

        if (string.IsNullOrEmpty(Receipt.GrnNumber))
        {
            Receipt.GrnNumber = await _numberRange.GetNextNumberAsync(tenantId, "MM", "GRN");
        }

        await _repo.AddAsync(Receipt);

        if (!string.IsNullOrEmpty(Receipt.PoNumber))
        {
            var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.PoNumber == Receipt.PoNumber);
            if (po != null)
            {
                var qtyReceived = decimal.TryParse(Receipt.QtyReceived, out var q) ? q : 0;
                var poItems = await _db.PurchaseOrderItems.Where(i => i.PurchaseOrderId == po.Id).ToListAsync();
                foreach (var item in poItems)
                {
                    item.ReceivedQty += qtyReceived;
                    item.Status = item.ReceivedQty >= item.Quantity ? "Received" : "Partially Received";
                }

                var totalReceived = poItems.Sum(i => i.ReceivedQty);
                var totalOrdered = poItems.Sum(i => i.Quantity);
                po.Status = totalReceived >= totalOrdered ? "Received" : "Partially Received";
                po.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        var batchNumber = Request.Form["BatchNumber"].FirstOrDefault();
        if (!string.IsNullOrEmpty(batchNumber))
        {
            var materialName = Receipt.MaterialName;
            var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == materialName);
            var qtyAccepted = decimal.TryParse(Receipt.QtyAccepted, out var qa) ? qa : 0;

            var batch = new BatchEntity
            {
                TenantId = tenantId,
                BatchNumber = batchNumber,
                MaterialId = material?.Id ?? Guid.Empty,
                MaterialName = materialName,
                ManufacturingDate = DateTime.UtcNow,
                Status = "ACTIVE",
                Quantity = qtyAccepted,
                UnitOfMeasure = material?.UOM ?? "EA",
            };
            _db.Batches.Add(batch);

            _db.StockItems.Add(new StockItemEntity
            {
                TenantId = tenantId,
                MaterialName = materialName,
                Lot = batchNumber,
                Quantity = qtyAccepted,
                UOM = material?.UOM ?? "EA",
                Value = qtyAccepted * (material?.Price ?? 0),
            });

            _db.StockMovements.Add(new StockMovementEntity
            {
                TenantId = tenantId,
                DocumentNumber = Receipt.GrnNumber,
                MaterialName = materialName,
                MovementType = "GR",
                Quantity = qtyAccepted,
                StockBefore = material?.Stock ?? 0,
                StockAfter = (material?.Stock ?? 0) + qtyAccepted,
                Reference = Receipt.PoNumber,
            });

            if (material != null)
            {
                material.Stock += qtyAccepted;
            }
        }

        var qiRequired = Request.Form["QualityInspectionRequired"].FirstOrDefault() == "true";
        if (qiRequired)
        {
            _db.InspectionLots.Add(new InspectionLotEntity
            {
                LotNumber = Receipt.GrnNumber,
                MaterialName = Receipt.MaterialName,
                Quantity = Receipt.QtyReceived,
                Status = "Pending"
            });
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("/MM/GRN/List");
    }
}
