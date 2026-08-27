namespace YuktiraERP.Core.Interfaces;

public interface IInventoryService
{
    Task<AtpResult> CheckAvailabilityAsync(Guid materialId, decimal requestedQty, DateTime deliveryDate);
    Task<decimal> GetAvailableQuantityAsync(Guid materialId);
    Task<ReservationResult> ReserveStockAsync(Guid materialId, decimal qty, Guid orderId);
    Task ReleaseReservationAsync(Guid reservationId);
    Task<CtpResult> GetConfirmedAvailabilityAsync(Guid materialId, string fromStore);
}

public class AtpResult
{
    public decimal AvailableQuantity { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal ScheduledReceipts { get; set; }
    public decimal SafetyStock { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime? EarliestDeliveryDate { get; set; }
}

public class ReservationResult
{
    public bool Success { get; set; }
    public Guid ReservationId { get; set; }
    public string Message { get; set; } = "";
}

public class CtpResult
{
    public decimal TotalAvailable { get; set; }
    public List<StoreAvailability> Stores { get; set; } = new();
}

public class StoreAvailability
{
    public string StoreCode { get; set; } = "";
    public string StoreName { get; set; } = "";
    public decimal AvailableQuantity { get; set; }
    public int LeadTimeDays { get; set; }
}
