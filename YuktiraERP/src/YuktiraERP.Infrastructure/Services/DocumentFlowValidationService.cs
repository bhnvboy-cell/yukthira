using System;
using System.Collections.Generic;
using System.Linq;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class DocumentFlowValidationService : IDocumentFlowValidationService
{
    public OrderThreeWayMatchEvaluation ValidateThreeWayMatch(
        IEnumerable<OrderThreeWayMatchLine> matchItems,
        decimal quantityTolerancePercent = 0m,
        decimal priceTolerancePercent = 0.5m)
    {
        var result = new OrderThreeWayMatchEvaluation { IsMatched = true };

        foreach (var item in matchItems)
        {
            // 1. Quantity Match: Invoiced Qty cannot exceed Received Qty (GRPO) beyond tolerance
            decimal allowedQty = item.GrpoQuantity * (1m + (quantityTolerancePercent / 100m));
            if (item.InvoiceQuantity > allowedQty)
            {
                result.IsMatched = false;
                decimal diff = item.InvoiceQuantity - item.GrpoQuantity;
                result.TotalQuantityDiscrepancy += diff;
                result.Discrepancies.Add(
                    $"Item '{item.ItemCode}': Invoiced quantity ({item.InvoiceQuantity}) exceeds Goods Receipt quantity ({item.GrpoQuantity}) by {diff}.");
            }

            // 2. Price Match: Invoiced Unit Price cannot exceed PO Unit Price beyond tolerance
            decimal maxAllowedPrice = item.PoUnitPrice * (1m + (priceTolerancePercent / 100m));
            if (item.InvoiceUnitPrice > maxAllowedPrice)
            {
                result.IsMatched = false;
                decimal priceVariance = (item.InvoiceUnitPrice - item.PoUnitPrice) * item.InvoiceQuantity;
                result.TotalPriceVariance += priceVariance;
                result.Discrepancies.Add(
                    $"Item '{item.ItemCode}': Invoiced price ({item.InvoiceUnitPrice}) exceeds PO price ({item.PoUnitPrice}) beyond {priceTolerancePercent}% tolerance.");
            }
        }

        return result;
    }

    public PrePostingValidationResult ValidateCustomerCreditLimit(
        decimal creditLimit,
        decimal currentBalance,
        decimal newOrderTotal,
        bool isCustomerOnHold = false)
    {
        var result = new PrePostingValidationResult { IsValid = true };

        if (isCustomerOnHold)
        {
            result.IsValid = false;
            result.ValidationErrors.Add("Customer account is currently on credit/operational hold.");
            return result;
        }

        if (creditLimit > 0m)
        {
            decimal projectedExposure = currentBalance + newOrderTotal;
            if (projectedExposure > creditLimit)
            {
                result.IsValid = false;
                decimal exceededBy = projectedExposure - creditLimit;
                result.ValidationErrors.Add(
                    $"Order total {newOrderTotal:C} brings customer balance to {projectedExposure:C}, exceeding credit limit {creditLimit:C} by {exceededBy:C}.");
            }
        }

        return result;
    }

    public PrePostingValidationResult ValidateInventoryAvailability(
        decimal requiredQty,
        decimal availableQty,
        string itemCode,
        string warehouseCode)
    {
        var result = new PrePostingValidationResult { IsValid = true };

        if (requiredQty > availableQty)
        {
            result.IsValid = false;
            decimal shortage = requiredQty - availableQty;
            result.ValidationErrors.Add(
                $"Item '{itemCode}' has insufficient stock in warehouse '{warehouseCode}'. Required: {requiredQty}, Available: {availableQty}, Shortage: {shortage}.");
        }

        return result;
    }
}
