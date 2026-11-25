using System;
using System.Collections.Generic;
using System.Linq;
using YuktiraERP.Core.Domain.Costing;
using YuktiraERP.Core.Domain.Finance;
using YuktiraERP.Core.Domain.Planning;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class FinanceCostingEngineService : IFinanceCostingEngineService
{
    public List<FxRevaluationResult> CalculateFxRevaluation(
        IEnumerable<ChartOfAccount> foreignAccounts,
        Func<Guid, decimal> getForeignBalance,
        Func<Guid, decimal> getBookValueFunctional,
        Func<string, decimal> getClosingRate)
    {
        var results = new List<FxRevaluationResult>();

        foreach (var account in foreignAccounts)
        {
            decimal foreignBalance = getForeignBalance(account.Id);
            if (foreignBalance == 0) continue;

            decimal bookValue = getBookValueFunctional(account.Id);
            decimal rate = getClosingRate(account.CurrencyCode);
            if (rate <= 0) continue;

            decimal revaluedFunctional = Math.Round(foreignBalance * rate, 4, MidpointRounding.AwayFromZero);

            results.Add(new FxRevaluationResult
            {
                AccountId = account.Id,
                AccountCode = account.AccountCode,
                CurrencyCode = account.CurrencyCode,
                ForeignCurrencyBalance = foreignBalance,
                BookValueFunctional = bookValue,
                ClosingRate = rate,
                RevaluedValueFunctional = revaluedFunctional
            });
        }

        return results;
    }

    public List<BudgetVarianceRecord> EvaluateBudgetVariances(
        IEnumerable<CostCenterBudget> budgets,
        Func<Guid, Guid, decimal> getActualSpend,
        Func<Guid, Guid, decimal> getCommittedSpend)
    {
        var results = new List<BudgetVarianceRecord>();

        foreach (var budget in budgets)
        {
            decimal actual = getActualSpend(budget.CostCenterId, budget.AccountId);
            decimal committed = getCommittedSpend(budget.CostCenterId, budget.AccountId);

            results.Add(new BudgetVarianceRecord
            {
                CostCenterId = budget.CostCenterId,
                AccountId = budget.AccountId,
                BudgetAmount = budget.BudgetAmount,
                ActualAmount = actual,
                CommittedAmount = committed
            });
        }

        return results;
    }

    public StandardCostVarianceReport CalculateCostVariance(
        Guid itemId,
        string itemCode,
        decimal quantity,
        decimal standardUnitCost,
        decimal actualUnitCost)
    {
        return new StandardCostVarianceReport
        {
            ItemId = itemId,
            ItemCode = itemCode,
            Quantity = quantity,
            StandardUnitCost = standardUnitCost,
            ActualUnitCost = actualUnitCost
        };
    }
}

public class MasterPlanningCalculationService : IMasterPlanningCalculationService
{
    public List<PlannedOrder> ExecuteMrpRun(
        IEnumerable<MrpDemandItem> demands,
        IEnumerable<MrpSupplyItem> currentSupplies,
        IEnumerable<BillOfMaterialHeader> bomCatalog,
        Func<Guid, bool> isManufacturedItem)
    {
        var supplyMap = currentSupplies.ToDictionary(
            s => s.ItemId,
            s => s.OnHandQty + s.ScheduledReceiptsQty);

        var bomsByItem = bomCatalog
            .Where(b => b.IsActive)
            .GroupBy(b => b.FinishedGoodItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.IsDefault).ThenByDescending(b => b.Version).First());

        var plannedOrders = new List<PlannedOrder>();
        var demandQueue = new Queue<MrpDemandItem>(demands);
        int plannedOrderCounter = 1;

        while (demandQueue.Count > 0)
        {
            var demand = demandQueue.Dequeue();
            supplyMap.TryGetValue(demand.ItemId, out decimal existingSupply);

            decimal netRequirement = demand.GrossRequirementQty - existingSupply;

            if (netRequirement > 0)
            {
                // Consume existing supply entirely
                supplyMap[demand.ItemId] = 0m;

                bool isMfg = isManufacturedItem(demand.ItemId);
                var order = new PlannedOrder
                {
                    OrderNumber = $"PLN-{plannedOrderCounter++:D6}",
                    ItemId = demand.ItemId,
                    ItemCode = demand.ItemCode,
                    OrderType = isMfg ? PlannedOrderType.Production : PlannedOrderType.Purchase,
                    Quantity = netRequirement,
                    DueDate = demand.RequirementDate,
                    StartDate = demand.RequirementDate.AddDays(isMfg ? -5 : -14),
                    DemandSource = demand.SourceReference
                };
                plannedOrders.Add(order);

                // If manufactured, explode BOM and enqueue component requirements
                if (isMfg && bomsByItem.TryGetValue(demand.ItemId, out var bom))
                {
                    decimal multiplier = (bom.BaseQuantity > 0) ? (netRequirement / bom.BaseQuantity) : netRequirement;

                    foreach (var component in bom.Components)
                    {
                        decimal compScrap = 1m + (component.ScrapPercent / 100m);
                        decimal neededQty = component.RequiredQuantity * multiplier * compScrap;

                        demandQueue.Enqueue(new MrpDemandItem
                        {
                            ItemId = component.ComponentItemId,
                            ItemCode = component.ComponentItemCode,
                            GrossRequirementQty = neededQty,
                            RequirementDate = order.StartDate, // Component needed when production starts
                            SourceReference = $"ParentPlannedOrder:{order.OrderNumber}"
                        });
                    }
                }
            }
            else
            {
                // Existing supply covers demand
                supplyMap[demand.ItemId] = existingSupply - demand.GrossRequirementQty;
            }
        }

        return plannedOrders;
    }
}
