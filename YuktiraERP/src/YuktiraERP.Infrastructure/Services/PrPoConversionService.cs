using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public interface IPrPoConversionService
{
    Task<PurchaseOrderEntity> ConvertPrToPoAsync(Guid prId, ConvertPrToPoRequest request, string userId);
    Task<List<PurchaseOrderEntity>> ConvertMultiplePrToPoAsync(List<Guid> prIds, string userId);
    Task<PrPoConversionResult> GetConversionPreviewAsync(Guid prId);
}

public class ConvertPrToPoRequest
{
    public string VendorName { get; set; } = "";
    public string VendorCode { get; set; } = "";
    public List<Guid>? SelectedItemIds { get; set; }
    public string PaymentTerms { get; set; } = "Net 30";
    public string Incoterms { get; set; } = "";
    public string DeliveryDate { get; set; } = "";
    public string Plant { get; set; } = "";
    public string Notes { get; set; } = "";
}

public class PrPoConversionResult
{
    public Guid PrId { get; set; }
    public string PrNumber { get; set; } = "";
    public int TotalItems { get; set; }
    public int SelectedItems { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PurchaseRequisitionItemEntity> Items { get; set; } = new();
}

public class PrPoConversionResultDto
{
    public PurchaseOrderEntity PurchaseOrder { get; set; } = new();
    public string Message { get; set; } = "";
}

public class PrPoConversionService : IPrPoConversionService
{
    private readonly YuktiraDbContext _db;
    private readonly INumberRangeService _numberRange;

    public PrPoConversionService(YuktiraDbContext db, INumberRangeService numberRange)
    {
        _db = db;
        _numberRange = numberRange;
    }

    public async Task<PrPoConversionResult> GetConversionPreviewAsync(Guid prId)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(prId)
            ?? throw new InvalidOperationException("Purchase Requisition not found.");

        var items = await _db.PurchaseRequisitionItems
            .Where(i => i.PurchaseRequisitionId == prId)
            .ToListAsync();

        return new PrPoConversionResult
        {
            PrId = pr.Id,
            PrNumber = pr.PrNumber,
            TotalItems = items.Count,
            SelectedItems = items.Count,
            TotalAmount = items.Sum(i => i.TotalPrice),
            Items = items
        };
    }

    public async Task<PurchaseOrderEntity> ConvertPrToPoAsync(Guid prId, ConvertPrToPoRequest request, string userId)
    {
        var pr = await _db.PurchaseRequisitions.FindAsync(prId)
            ?? throw new InvalidOperationException("Purchase Requisition not found.");

        if (pr.Status != "APPROVED")
            throw new InvalidOperationException("PR must be APPROVED before conversion to PO.");

        var allItems = await _db.PurchaseRequisitionItems
            .Where(i => i.PurchaseRequisitionId == prId && i.Status == "OPEN")
            .ToListAsync();

        var itemsToConvert = request.SelectedItemIds != null && request.SelectedItemIds.Count > 0
            ? allItems.Where(i => request.SelectedItemIds.Contains(i.Id)).ToList()
            : allItems;

        if (itemsToConvert.Count == 0)
            throw new InvalidOperationException("No items selected for conversion.");

        var tenantId = pr.TenantId != Guid.Empty ? pr.TenantId : _db.TenantId ?? Guid.Empty;
        var poNumber = await _numberRange.GetNextNumberAsync(tenantId, "MM", "PO");

        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = poNumber,
            Date = DateTime.UtcNow,
            VendorName = request.VendorName,
            VendorCode = request.VendorCode,
            ItemName = itemsToConvert.First().MaterialName,
            Quantity = itemsToConvert.Sum(i => i.Quantity).ToString(),
            Amount = itemsToConvert.Sum(i => i.TotalPrice),
            TotalAmount = itemsToConvert.Sum(i => i.TotalPrice),
            ItemCount = itemsToConvert.Count,
            PaymentTerms = request.PaymentTerms,
            Incoterms = request.Incoterms,
            Status = "DRAFT",
            DepartmentKey = pr.DepartmentKey,
            CostCenter = pr.CostCenter,
        };

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();

        int lineNum = 1;
        foreach (var prItem in itemsToConvert)
        {
            var poItem = new PurchaseOrderItemEntity
            {
                TenantId = tenantId,
                PurchaseOrderId = po.Id,
                LineNumber = lineNum++,
                MaterialName = prItem.MaterialName,
                MaterialCode = prItem.MaterialCode,
                Quantity = prItem.Quantity,
                UOM = prItem.UOM,
                UnitPrice = prItem.UnitPrice,
                TotalPrice = prItem.TotalPrice,
                Plant = prItem.Plant,
                StorageLocation = prItem.StorageLocation,
                DeliveryDate = string.IsNullOrEmpty(request.DeliveryDate) ? prItem.DeliveryDate : request.DeliveryDate,
                Status = "OPEN",
                DepartmentKey = prItem.DepartmentKey,
                CostCenter = prItem.CostCenter,
            };
            _db.PurchaseOrderItems.Add(poItem);

            prItem.Status = "CONVERTED";
        }

        pr.Status = "CONVERTED";
        pr.ConvertedPoNumber = poNumber;

        await _db.SaveChangesAsync();
        return po;
    }

    public async Task<List<PurchaseOrderEntity>> ConvertMultiplePrToPoAsync(List<Guid> prIds, string userId)
    {
        var results = new List<PurchaseOrderEntity>();
        foreach (var prId in prIds)
        {
            var request = new ConvertPrToPoRequest();
            var po = await ConvertPrToPoAsync(prId, request, userId);
            results.Add(po);
        }
        return results;
    }
}
