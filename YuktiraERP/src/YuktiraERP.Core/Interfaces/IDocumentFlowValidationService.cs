using System;
using System.Collections.Generic;

namespace YuktiraERP.Core.Interfaces;

public class OrderThreeWayMatchLine
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public decimal PoQuantity { get; set; }
    public decimal PoUnitPrice { get; set; }
    public decimal GrpoQuantity { get; set; }
    public decimal InvoiceQuantity { get; set; }
    public decimal InvoiceUnitPrice { get; set; }
}

public class OrderThreeWayMatchEvaluation
{
    public bool IsMatched { get; set; }
    public List<string> Discrepancies { get; set; } = new List<string>();
    public decimal TotalQuantityDiscrepancy { get; set; }
    public decimal TotalPriceVariance { get; set; }
}

public class PrePostingValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new List<string>();
}

public interface IDocumentFlowValidationService
{
    /// <summary>
    /// Performs 3-way matching between Purchase Order, Goods Receipt PO, and AP Invoice lines.
    /// Checks quantity tolerances and unit price tolerances.
    /// </summary>
    OrderThreeWayMatchEvaluation ValidateThreeWayMatch(
        IEnumerable<OrderThreeWayMatchLine> matchItems,
        decimal quantityTolerancePercent = 0m,
        decimal priceTolerancePercent = 0.5m);

    /// <summary>
    /// Validates customer credit limit against current balance + pending order total.
    /// </summary>
    PrePostingValidationResult ValidateCustomerCreditLimit(
        decimal creditLimit,
        decimal currentBalance,
        decimal newOrderTotal,
        bool isCustomerOnHold = false);

    /// <summary>
    /// Validates available-to-promise (ATP) stock in a warehouse before dispatch.
    /// </summary>
    PrePostingValidationResult ValidateInventoryAvailability(
        decimal requiredQty,
        decimal availableQty,
        string itemCode,
        string warehouseCode);
}
