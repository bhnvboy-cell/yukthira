using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.PP;

[Authorize]
public class MrpStockModel : PageModel
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public MrpStockModel(YuktiraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public List<MaterialStockAnalysis> MaterialAnalysis { get; set; } = new();

    public async Task OnGetAsync()
    {
        var materials = await _db.MaterialMasters.Where(m => m.Status == "Active").ToListAsync();
        var stockItems = await _db.StockItems.ToListAsync();
        var openPOs = await _db.PurchaseOrders
            .Where(p => p.Status != "Completed" && p.Status != "Cancelled")
            .ToListAsync();
        var openProductionOrders = await _db.ProductionOrders
            .Where(o => o.Status != "COMPLETED" && o.Status != "TECO" && o.Status != "CANCELLED")
            .ToListAsync();
        var boms = await _db.BillOfMaterials
            .Where(b => b.Status == "Active")
            .ToListAsync();
        var reservations = await _db.Set<StockReservationEntity>()
            .Where(r => r.Status == "Active")
            .ToListAsync();

        foreach (var mat in materials)
        {
            var currentStock = stockItems.Where(s => s.MaterialName == mat.Name).Sum(s => s.Quantity);
            var safetyStock = stockItems.Where(s => s.MaterialName == mat.Name).Sum(s => s.MinStock);

            decimal openPOQty = 0;
            foreach (var po in openPOs.Where(p => p.ItemName == mat.Name))
            {
                if (decimal.TryParse(po.Quantity?.Split(' ')[0], out var qty))
                    openPOQty += qty;
            }

            decimal openProdQty = 0;
            foreach (var order in openProductionOrders)
            {
                var components = boms.Where(b => b.ProductName == order.ProductName).ToList();
                openProdQty += components.Where(c => c.ComponentName == mat.Name).Sum(c => order.Quantity * c.Quantity);
            }

            var reservedQty = reservations.Where(r => r.MaterialName == mat.Name).Sum(r => r.Quantity);
            var netRequirement = Math.Max(0, safetyStock + openProdQty - currentStock - openPOQty + reservedQty);

            string suggestedAction;
            if (currentStock <= 0 && openPOQty == 0)
                suggestedAction = "CRITICAL: No stock and no open POs - create purchase requisition immediately";
            else if (netRequirement > 0)
                suggestedAction = $"Create procurement for {netRequirement} units";
            else if (currentStock < safetyStock)
                suggestedAction = $"Stock below safety level ({currentStock}/{safetyStock}) - monitor closely";
            else
                suggestedAction = "Stock levels adequate";

            MaterialAnalysis.Add(new MaterialStockAnalysis
            {
                MaterialCode = mat.Code,
                MaterialName = mat.Name,
                MaterialType = mat.Type,
                CurrentStock = currentStock,
                SafetyStock = safetyStock,
                OpenPOQty = openPOQty,
                OpenProductionQty = openProdQty,
                ReservedQty = reservedQty,
                NetRequirement = netRequirement,
                SuggestedAction = suggestedAction
            });
        }

        MaterialAnalysis = MaterialAnalysis.OrderByDescending(m => m.NetRequirement).ToList();
    }
}

public class MaterialStockAnalysis
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string MaterialType { get; set; } = "";
    public decimal CurrentStock { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal OpenPOQty { get; set; }
    public decimal OpenProductionQty { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal NetRequirement { get; set; }
    public string SuggestedAction { get; set; } = "";
}
