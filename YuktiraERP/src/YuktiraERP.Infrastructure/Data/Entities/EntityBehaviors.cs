namespace YuktiraERP.Infrastructure.Data.Entities;

// Domain behavior: encapsulated business rules on key entities.
// Controllers/services call these methods instead of manipulating state directly.

public partial class SalesOrderLineEntity
{
    public void SetPricing(decimal quantity, decimal unitPrice)
    {
        if (quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero");
        if (unitPrice < 0) throw new InvalidOperationException("Unit price cannot be negative");
        Quantity = quantity;
        UnitPrice = unitPrice;
        TotalPrice = Math.Round(quantity * unitPrice, 2);
    }
}

public partial class FixedAssetEntity
{
    private decimal DepreciableBase => Math.Max(0, Cost - SalvageValue);

    /// <summary>Straight-line depreciation per full year.</summary>
    public decimal AnnualDepreciation()
        => UsefulLifeYears <= 0 ? 0 : Math.Round(DepreciableBase / UsefulLifeYears, 2);

    /// <summary>Pro-rata depreciation from purchase date to the given date.</summary>
    public decimal DepreciationTo(DateTime asOf)
    {
        if (asOf <= PurchaseDate) return 0;
        var fullYears = (asOf - PurchaseDate).Days / 365.0;
        return Math.Round(AnnualDepreciation() * (decimal)fullYears, 2);
    }

    /// <summary>Current net book value at a point in time.</summary>
    public decimal BookValue(DateTime asOf)
        => Math.Max(0, Cost - DepreciationTo(asOf));

    public void MarkActive() => Status = "Active";
    public void MarkScrapped() => Status = "Scrapped";
    public void MarkTransferred() => Status = "Transferred";

    /// <summary>Validate the depreciation inputs before persisting.</summary>
    public void ValidateLifecycle()
    {
        if (Cost < 0) throw new InvalidOperationException("Asset cost cannot be negative");
        if (SalvageValue < 0) throw new InvalidOperationException("Salvage value cannot be negative");
        if (SalvageValue > Cost) throw new InvalidOperationException("Salvage value cannot exceed cost");
        if (UsefulLifeYears <= 0) throw new InvalidOperationException("Useful life must be at least 1 year");
    }
}

public partial class AREntryEntity
{
    public decimal OutstandingAmount => Math.Max(0, Amount - ReceivedAmount);
    public bool IsFullyReceived => OutstandingAmount <= 0;

    public void ApplyReceipt(decimal amount)
    {
        if (amount < 0) throw new InvalidOperationException("Receipt amount cannot be negative");
        if (amount > OutstandingAmount) throw new InvalidOperationException("Receipt exceeds outstanding amount");
        ReceivedAmount += amount;
        if (IsFullyReceived) Status = "Closed";
    }
}

public partial class PurchaseOrderEntity
{
    private static readonly HashSet<string> AllowedTransitions = new()
    {
        "Pending->Approved", "Pending->Rejected", "Approved->Received",
        "Approved->Partially Received", "Partially Received->Received",
        "Partially Received->Approved", "Received->Closed"
    };

    public bool CanTransitionTo(string newStatus)
        => AllowedTransitions.Contains($"{Status}->{newStatus}");

    public void TransitionTo(string newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Invalid status transition '{Status}' -> '{newStatus}'");
        Status = newStatus;
    }
}

public partial class PurchaseRequisitionEntity
{
    private static readonly HashSet<string> AllowedTransitions = new()
    {
        "Pending->Approved", "Pending->Rejected", "Approved->Converted to PO"
    };

    public bool CanTransitionTo(string newStatus)
        => AllowedTransitions.Contains($"{Status}->{newStatus}");

    public void TransitionTo(string newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Invalid status transition '{Status}' -> '{newStatus}'");
        Status = newStatus;
    }
}

public partial class DeliveryEntity
{
    private static readonly HashSet<string> AllowedTransitions = new()
    {
        "Picked->Shipped", "Shipped->Delivered", "Picked->Cancelled", "Shipped->Cancelled"
    };

    public bool CanTransitionTo(string newStatus)
        => AllowedTransitions.Contains($"{Status}->{newStatus}");

    public void TransitionTo(string newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Invalid status transition '{Status}' -> '{newStatus}'");
        Status = newStatus;
    }
}