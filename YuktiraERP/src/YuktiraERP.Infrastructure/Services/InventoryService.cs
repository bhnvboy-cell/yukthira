using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly YuktiraDbContext _db;

    public InventoryService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<AtpResult> CheckAvailabilityAsync(Guid materialId, decimal requestedQty, DateTime deliveryDate)
    {
        var material = await _db.MaterialMasters.FindAsync(materialId);
        if (material == null)
            return new AtpResult { IsAvailable = false };

        var stockItem = await _db.StockItems
            .FirstOrDefaultAsync(s => s.MaterialName == material.Name);

        var reservedQty = await _db.Set<StockReservationEntity>()
            .Where(r => r.MaterialId == materialId && r.Status == "Active")
            .SumAsync(r => r.Quantity);

        var allocatedQty = await _db.Set<StockAllocationEntity>()
            .Where(a => a.MaterialId == materialId && a.Status == "Allocated")
            .SumAsync(a => a.Quantity);

        var scheduledReceipts = (await _db.PurchaseOrders
            .Where(po => po.Status == "Ordered" && po.Quantity != null)
            .Select(po => po.Quantity)
            .ToListAsync())
            .Where(q => q != null && decimal.TryParse(q!.Split(' ')[0], out _))
            .Sum(q => decimal.Parse(q!.Split(' ')[0]));

        var currentStock = material.Stock;
        var atp = currentStock - reservedQty + scheduledReceipts - allocatedQty;
        var safetyStock = stockItem?.MinStock ?? 0;

        return new AtpResult
        {
            AvailableQuantity = atp,
            AllocatedQuantity = allocatedQty,
            ScheduledReceipts = scheduledReceipts,
            SafetyStock = safetyStock,
            IsAvailable = atp >= requestedQty && (atp - requestedQty) >= safetyStock,
            EarliestDeliveryDate = CalculateEarliestDelivery(deliveryDate)
        };
    }

    public async Task<decimal> GetAvailableQuantityAsync(Guid materialId)
    {
        var material = await _db.MaterialMasters.FindAsync(materialId);
        if (material == null) return 0;

        var reservedQty = await _db.Set<StockReservationEntity>()
            .Where(r => r.MaterialId == materialId && r.Status == "Active")
            .SumAsync(r => r.Quantity);

        var allocatedQty = await _db.Set<StockAllocationEntity>()
            .Where(a => a.MaterialId == materialId && a.Status == "Allocated")
            .SumAsync(a => a.Quantity);

        return material.Stock - reservedQty - allocatedQty;
    }

    public async Task<ReservationResult> ReserveStockAsync(Guid materialId, decimal qty, Guid orderId)
    {
        var material = await _db.MaterialMasters.FindAsync(materialId);
        if (material == null)
            return new ReservationResult { Success = false, Message = "Material not found" };

        var available = await GetAvailableQuantityAsync(materialId);
        if (available < qty)
            return new ReservationResult { Success = false, Message = $"Insufficient stock. Available: {available}, Requested: {qty}" };

        var reservation = new StockReservationEntity
        {
            MaterialId = materialId,
            MaterialName = material.Name,
            Quantity = qty,
            OrderId = orderId,
            Status = "Active",
            ReservedAt = DateTime.UtcNow
        };

        _db.Set<StockReservationEntity>().Add(reservation);
        await _db.SaveChangesAsync();

        return new ReservationResult
        {
            Success = true,
            ReservationId = reservation.Id,
            Message = "Stock reserved successfully"
        };
    }

    public async Task ReleaseReservationAsync(Guid reservationId)
    {
        var reservation = await _db.Set<StockReservationEntity>().FindAsync(reservationId);
        if (reservation == null) return;

        reservation.Status = "Released";
        reservation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<CtpResult> GetConfirmedAvailabilityAsync(Guid materialId, string fromStore)
    {
        var material = await _db.MaterialMasters.FindAsync(materialId);
        if (material == null)
            return new CtpResult { TotalAvailable = 0 };

        var stockItems = await _db.StockItems
            .Where(s => s.MaterialName == material.Name)
            .ToListAsync();

        var stores = new List<StoreAvailability>();
        decimal totalAvailable = 0;

        foreach (var stock in stockItems)
        {
            var storeReservedQty = await _db.Set<StockReservationEntity>()
                .Where(r => r.MaterialId == materialId && r.Status == "Active")
                .SumAsync(r => (decimal?)r.Quantity) ?? 0m;
            var storeAllocatedQty = await _db.Set<StockAllocationEntity>()
                .Where(a => a.MaterialId == materialId && a.Status == "Allocated")
                .SumAsync(a => (decimal?)a.Quantity) ?? 0m;

            var available = stock.Quantity - storeReservedQty - storeAllocatedQty;
            stores.Add(new StoreAvailability
            {
                StoreCode = stock.Bin,
                StoreName = stock.Bin,
                AvailableQuantity = available,
                LeadTimeDays = 0
            });
            totalAvailable += available;
        }

        return new CtpResult
        {
            TotalAvailable = totalAvailable,
            Stores = stores
        };
    }

    private static DateTime CalculateEarliestDelivery(DateTime requestedDate)
    {
        var leadTimeDays = 7;
        var estimatedDelivery = DateTime.UtcNow.AddDays(leadTimeDays);
        return requestedDate > estimatedDelivery ? requestedDate : estimatedDelivery;
    }
}
