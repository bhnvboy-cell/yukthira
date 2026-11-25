using System;
using System.Collections.Generic;
using YuktiraERP.Core.Domain.Costing;
using YuktiraERP.Core.Domain.Finance;
using YuktiraERP.Core.Domain.Planning;

namespace YuktiraERP.Core.Interfaces;

public interface IFinanceCostingEngineService
{
    /// <summary>
    /// Performs Foreign Exchange (FX) revaluation on foreign currency accounts at period close.
    /// Calculates unrealized gains or losses.
    /// </summary>
    List<FxRevaluationResult> CalculateFxRevaluation(
        IEnumerable<ChartOfAccount> foreignAccounts,
        Func<Guid, decimal> getForeignBalance,
        Func<Guid, decimal> getBookValueFunctional,
        Func<string, decimal> getClosingRate);

    /// <summary>
    /// Calculates Budget vs Actual variances for cost centers, considering actual and committed spend.
    /// </summary>
    List<BudgetVarianceRecord> EvaluateBudgetVariances(
        IEnumerable<CostCenterBudget> budgets,
        Func<Guid, Guid, decimal> getActualSpend,
        Func<Guid, Guid, decimal> getCommittedSpend);

    /// <summary>
    /// Calculates standard vs actual variance on production or purchased quantities.
    /// </summary>
    StandardCostVarianceReport CalculateCostVariance(
        Guid itemId,
        string itemCode,
        decimal quantity,
        decimal standardUnitCost,
        decimal actualUnitCost);
}

public interface IMasterPlanningCalculationService
{
    /// <summary>
    /// Computes Net Requirements and generates Planned Orders:
    /// Net Requirement = Gross Requirement - (On Hand + Scheduled Receipts)
    /// Also explodes multi-level BOM components recursively.
    /// </summary>
    List<PlannedOrder> ExecuteMrpRun(
        IEnumerable<MrpDemandItem> demands,
        IEnumerable<MrpSupplyItem> currentSupplies,
        IEnumerable<BillOfMaterialHeader> bomCatalog,
        Func<Guid, bool> isManufacturedItem);
}
