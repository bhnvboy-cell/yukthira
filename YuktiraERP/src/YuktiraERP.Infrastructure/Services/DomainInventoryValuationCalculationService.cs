using System;
using System.Collections.Generic;
using System.Linq;
using YuktiraERP.Core.Domain.Inventory;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class DomainInventoryValuationCalculationService : IInventoryValuationCalculationService
{
    public (decimal NewQty, decimal NewTotalValue, decimal NewMovingAvg) CalculateMovingAverageReceipt(
        decimal currentOnHand,
        decimal currentMovingAvg,
        decimal receiptQty,
        decimal receiptUnitCost)
    {
        if (receiptQty <= 0)
            throw new ArgumentOutOfRangeException(nameof(receiptQty), "Receipt quantity must be greater than zero.");

        decimal currentTotalValue = Math.Max(0m, currentOnHand * currentMovingAvg);
        decimal incomingValue = receiptQty * receiptUnitCost;
        decimal newQty = currentOnHand + receiptQty;

        if (newQty <= 0m)
        {
            return (0m, 0m, 0m);
        }

        decimal newTotalValue = currentTotalValue + incomingValue;
        decimal newMovingAvg = Math.Round(newTotalValue / newQty, 6, MidpointRounding.AwayFromZero);

        return (newQty, newTotalValue, newMovingAvg);
    }

    public List<BatchDepletionResult> DepleteBatches(
        IEnumerable<ItemBatch> availableBatches,
        decimal requiredQty,
        bool useFefo = true)
    {
        if (requiredQty <= 0)
            throw new ArgumentOutOfRangeException(nameof(requiredQty), "Required quantity must be greater than zero.");

        var candidates = availableBatches
            .Where(b => !b.IsLocked && b.AvailableQty > 0);

        var orderedBatches = useFefo
            ? candidates.OrderBy(b => b.ExpiryDate).ThenBy(b => b.ManufacturingDate).ToList()
            : candidates.OrderBy(b => b.ManufacturingDate).ThenBy(b => b.CreatedAtUtc).ToList();

        decimal totalAvailable = orderedBatches.Sum(b => b.AvailableQty);
        if (totalAvailable < requiredQty)
        {
            throw new InvalidOperationException(
                $"Insufficient batch stock available. Required: {requiredQty}, Available: {totalAvailable}.");
        }

        var results = new List<BatchDepletionResult>();
        decimal qtyNeeded = requiredQty;

        foreach (var batch in orderedBatches)
        {
            if (qtyNeeded <= 0) break;

            decimal qtyToTake = Math.Min(batch.AvailableQty, qtyNeeded);
            batch.QtyOnHand -= qtyToTake;
            qtyNeeded -= qtyToTake;

            // Zero-quantity batch auto-hiding
            batch.UpdateHiddenState();

            results.Add(new BatchDepletionResult
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate,
                DepletedQty = qtyToTake,
                UnitCost = batch.UnitCost,
                RemainingBatchQty = batch.QtyOnHand,
                BatchAutoHidden = batch.IsHidden
            });
        }

        return results;
    }
}
