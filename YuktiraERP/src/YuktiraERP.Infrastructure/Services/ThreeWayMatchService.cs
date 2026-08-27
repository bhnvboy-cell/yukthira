using Microsoft.EntityFrameworkCore;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IThreeWayMatchService
{
    Task<ThreeWayMatchResult> PerformMatchAsync(Guid invoiceId);
    Task<ThreeWayMatchResult> PerformMatchWithToleranceAsync(Guid invoiceId, decimal priceTolerancePercent, decimal qtyTolerancePercent);
}

public class ThreeWayMatchResult
{
    public bool IsMatch { get; set; }
    public decimal PriceVariance { get; set; }
    public decimal QuantityVariance { get; set; }
    public bool PriceWithinTolerance { get; set; }
    public bool QuantityWithinTolerance { get; set; }
    public List<MatchLineDetail> LineDetails { get; set; } = new();
    public string OverallStatus { get; set; } = "";
}

public class MatchLineDetail
{
    public string MaterialName { get; set; } = "";
    public decimal InvoicePrice { get; set; }
    public decimal POPrice { get; set; }
    public decimal InvoiceQty { get; set; }
    public decimal POReceivedQty { get; set; }
    public decimal PriceVariancePercent { get; set; }
    public decimal QuantityVariancePercent { get; set; }
    public bool IsPriceMatch { get; set; }
    public bool IsQtyMatch { get; set; }
    public string Status { get; set; } = "";
}

public class ThreeWayMatchService : IThreeWayMatchService
{
    private readonly YuktiraDbContext _db;

    public ThreeWayMatchService(YuktiraDbContext db) { _db = db; }

    public async Task<ThreeWayMatchResult> PerformMatchAsync(Guid invoiceId)
    {
        return await PerformMatchWithToleranceAsync(invoiceId, 5m, 5m);
    }

    public async Task<ThreeWayMatchResult> PerformMatchWithToleranceAsync(Guid invoiceId, decimal priceTolerancePercent, decimal qtyTolerancePercent)
    {
        var invoice = await _db.InvoiceVerifications.FindAsync(invoiceId)
            ?? throw new InvalidOperationException("Invoice not found.");

        var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.PoNumber == invoice.PoNumber)
            ?? throw new InvalidOperationException("Purchase Order not found for this invoice.");

        var poItems = await _db.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == po.Id)
            .ToListAsync();

        var result = new ThreeWayMatchResult { LineDetails = new List<MatchLineDetail>() };

        if (poItems.Count == 0)
        {
            result.IsMatch = false;
            result.OverallStatus = "NO_PO_LINES";
            return result;
        }

        decimal totalInvoiceAmount = invoice.Amount;
        decimal totalPOAmount = poItems.Sum(i => i.TotalPrice);

        decimal priceVariance = totalPOAmount != 0
            ? Math.Abs(totalInvoiceAmount - totalPOAmount) / totalPOAmount * 100
            : 0;

        result.PriceVariance = priceVariance;
        result.PriceWithinTolerance = priceVariance <= priceTolerancePercent;

        foreach (var poItem in poItems)
        {
            var lineDetail = new MatchLineDetail
            {
                MaterialName = poItem.MaterialName,
                POPrice = poItem.UnitPrice,
                InvoicePrice = invoice.Amount / poItems.Count,
                POReceivedQty = poItem.ReceivedQty,
                InvoiceQty = invoice.Amount / (poItem.UnitPrice > 0 ? poItem.UnitPrice : 1),
            };

            lineDetail.PriceVariancePercent = lineDetail.POPrice != 0
                ? Math.Abs(lineDetail.InvoicePrice - lineDetail.POPrice) / lineDetail.POPrice * 100
                : 0;

            lineDetail.QuantityVariancePercent = lineDetail.POReceivedQty != 0
                ? Math.Abs(lineDetail.InvoiceQty - lineDetail.POReceivedQty) / lineDetail.POReceivedQty * 100
                : 0;

            lineDetail.IsPriceMatch = lineDetail.PriceVariancePercent <= priceTolerancePercent;
            lineDetail.IsQtyMatch = lineDetail.QuantityVariancePercent <= qtyTolerancePercent;
            lineDetail.Status = lineDetail.IsPriceMatch && lineDetail.IsQtyMatch ? "MATCHED" : "VARIANCE";

            result.LineDetails.Add(lineDetail);
        }

        result.QuantityWithinTolerance = result.LineDetails.All(l => l.IsQtyMatch);
        result.QuantityVariance = result.LineDetails.Any() ? result.LineDetails.Average(l => l.QuantityVariancePercent) : 0;

        result.IsMatch = result.PriceWithinTolerance && result.QuantityWithinTolerance;
        result.OverallStatus = result.IsMatch ? "MATCHED"
            : !result.PriceWithinTolerance ? "PRICE_VARIANCE"
            : !result.QuantityWithinTolerance ? "QTY_VARIANCE"
            : "MISMATCH";

        return result;
    }
}
