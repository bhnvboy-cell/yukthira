using System;
using System.Collections.Generic;
using YuktiraERP.Core.Domain.Inventory;
using YuktiraERP.Core.Domain.MasterData;

namespace YuktiraERP.Core.Interfaces;

public interface IInventoryValuationCalculationService
{
    /// <summary>
    /// Recalculates Moving Average Cost upon Goods Receipt:
    /// NewAvg = (CurrentOnHand * CurrentAvg + ReceivedQty * ReceivedCost) / (CurrentOnHand + ReceivedQty)
    /// </summary>
    (decimal NewQty, decimal NewTotalValue, decimal NewMovingAvg) CalculateMovingAverageReceipt(
        decimal currentOnHand,
        decimal currentMovingAvg,
        decimal receiptQty,
        decimal receiptUnitCost);

    /// <summary>
    /// Selects batches using First-Expired-First-Out (FEFO) or FIFO strategy,
    /// depleting quantities across multiple batches and marking zero-quantity batches as IsHidden.
    /// </summary>
    List<BatchDepletionResult> DepleteBatches(
        IEnumerable<ItemBatch> availableBatches,
        decimal requiredQty,
        bool useFefo = true);
}
