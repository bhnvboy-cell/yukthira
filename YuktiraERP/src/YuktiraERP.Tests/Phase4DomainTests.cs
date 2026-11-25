using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Domain.Costing;
using YuktiraERP.Core.Domain.Finance;
using YuktiraERP.Core.Domain.Planning;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class Phase4DomainTests
{
    private readonly IFinanceCostingEngineService _financeService = new FinanceCostingEngineService();
    private readonly IMasterPlanningCalculationService _mrpService = new MasterPlanningCalculationService();
    private readonly IReportingQueryService _queryService = new ReportingQueryService();

    [Fact]
    public void JournalEntry_DoubleEntryBalance_BalancedEntrySucceeds()
    {
        var entry = new JournalEntry
        {
            EntryNumber = "JV-2026-0001",
            PostingDate = new DateOnly(2026, 11, 25),
            Status = GenericDocumentState.Draft
        };

        entry.Lines.Add(new JournalEntryLine
        {
            LineNumber = 1,
            AccountId = Guid.NewGuid(),
            AccountCode = "101000", // Cash / Bank
            DebitAmount = 5000m,
            CreditAmount = 0m
        });

        entry.Lines.Add(new JournalEntryLine
        {
            LineNumber = 2,
            AccountId = Guid.NewGuid(),
            AccountCode = "401000", // Sales Revenue
            DebitAmount = 0m,
            CreditAmount = 5000m
        });

        // Invariant check must succeed without exception
        entry.AssertDoubleEntryBalance();
        Assert.Equal(5000m, entry.TotalDebit);
        Assert.Equal(5000m, entry.TotalCredit);
    }

    [Fact]
    public void JournalEntry_DoubleEntryBalance_UnbalancedThrowsException()
    {
        var entry = new JournalEntry
        {
            EntryNumber = "JV-2026-0002",
            Status = GenericDocumentState.Draft
        };

        entry.Lines.Add(new JournalEntryLine
        {
            LineNumber = 1,
            AccountId = Guid.NewGuid(),
            AccountCode = "101000",
            DebitAmount = 5000m,
            CreditAmount = 0m
        });

        entry.Lines.Add(new JournalEntryLine
        {
            LineNumber = 2,
            AccountId = Guid.NewGuid(),
            AccountCode = "401000",
            DebitAmount = 0m,
            CreditAmount = 4500m // Discrepancy of 500
        });

        var ex = Assert.Throws<InvalidOperationException>(() => entry.AssertDoubleEntryBalance());
        Assert.Contains("Double-entry invariant violated", ex.Message);
    }

    [Fact]
    public void JournalEntry_Immutability_PostedEntryBlocksMutation()
    {
        var entry = new JournalEntry
        {
            EntryNumber = "JV-2026-0003",
            Status = GenericDocumentState.Posted
        };

        var ex = Assert.Throws<InvalidOperationException>(() => entry.AssertCanMutate());
        Assert.Contains("is immutable", ex.Message);
    }

    [Fact]
    public void FiscalPeriod_Locking_EnforcesSoftAndHardLocks()
    {
        var softPeriod = new FiscalCalendarPeriod
        {
            FiscalYear = 2026,
            PeriodNumber = 10,
            Status = FiscalPeriodStatus.SoftLocked
        };

        // Standard posting in SoftLocked period fails
        var ex1 = Assert.Throws<InvalidOperationException>(() => softPeriod.AssertOpenForPosting(isAdjustmentEntry: false));
        Assert.Contains("Soft Locked", ex1.Message);

        // Adjustment entry in SoftLocked period succeeds
        softPeriod.AssertOpenForPosting(isAdjustmentEntry: true);

        var hardPeriod = new FiscalCalendarPeriod
        {
            FiscalYear = 2026,
            PeriodNumber = 9,
            Status = FiscalPeriodStatus.HardLocked
        };

        // Even adjustment in HardLocked period fails
        var ex2 = Assert.Throws<InvalidOperationException>(() => hardPeriod.AssertOpenForPosting(isAdjustmentEntry: true));
        Assert.Contains("Hard Locked", ex2.Message);
    }

    [Fact]
    public void FxRevaluation_CalculatesUnrealizedGainOrLoss()
    {
        var accId = Guid.NewGuid();
        var foreignAccounts = new List<ChartOfAccount>
        {
            new() { Id = accId, AccountCode = "102000", CurrencyCode = "EUR" }
        };

        // Foreign Balance: 10,000 EUR
        // Book value functional (USD recorded at 1.05 rate): $10,500 USD
        // Closing rate: 1 EUR = 1.10 USD
        // Revalued functional: $11,000 USD
        // Expected Unrealized Gain: +$500 USD
        var results = _financeService.CalculateFxRevaluation(
            foreignAccounts,
            getForeignBalance: id => 10000m,
            getBookValueFunctional: id => 10500m,
            getClosingRate: curr => 1.10m);

        Assert.Single(results);
        var res = results[0];
        Assert.Equal(10000m, res.ForeignCurrencyBalance);
        Assert.Equal(10500m, res.BookValueFunctional);
        Assert.Equal(11000m, res.RevaluedValueFunctional);
        Assert.Equal(500m, res.UnrealizedGainOrLoss);
    }

    [Fact]
    public void BudgetVariance_IdentifiesOverBudgetAndUtilization()
    {
        var ccId = Guid.NewGuid();
        var accId = Guid.NewGuid();
        var budgets = new List<CostCenterBudget>
        {
            new()
            {
                CostCenterId = ccId,
                AccountId = accId,
                FiscalYear = 2026,
                FiscalPeriod = 11,
                BudgetAmount = 10000m
            }
        };

        // Actual spend: $8,500, Committed spend (open POs): $2,000 => Total exposure: $10,500 > $10,000
        var variances = _financeService.EvaluateBudgetVariances(
            budgets,
            getActualSpend: (c, a) => 8500m,
            getCommittedSpend: (c, a) => 2000m);

        Assert.Single(variances);
        var v = variances[0];
        Assert.Equal(10000m, v.BudgetAmount);
        Assert.Equal(8500m, v.ActualAmount);
        Assert.Equal(2000m, v.CommittedAmount);
        Assert.Equal(10500m, v.TotalExposure);
        Assert.Equal(-500m, v.Variance);
        Assert.Equal(105m, v.UtilizationPercent);
        Assert.True(v.IsOverBudget);
    }

    [Fact]
    public void StandardCostVariance_ComputesPriceVarianceAccurately()
    {
        var itemId = Guid.NewGuid();
        // Quantity 200, Standard unit cost $50, Actual unit cost $55 => Variance = +$1,000 unfavorable
        var report = _financeService.CalculateCostVariance(itemId, "SKU-VALVE-01", 200m, 50m, 55m);

        Assert.Equal(10000m, report.TotalStandardCost);
        Assert.Equal(11000m, report.TotalActualCost);
        Assert.Equal(1000m, report.CostVariance);
        Assert.False(report.IsFavorable);
    }

    [Fact]
    public void MasterPlanning_ExplodesMultiLevelBomAndGeneratesPlannedOrders()
    {
        var fgId = Guid.NewGuid();
        var subAssyId = Guid.NewGuid();
        var rawMatId = Guid.NewGuid();

        // BOM 1: Finished Good requires 2 Sub-Assemblies
        var fgBom = new BillOfMaterialHeader
        {
            FinishedGoodItemId = fgId,
            BomCode = "BOM-FG",
            BaseQuantity = 1m,
            IsActive = true
        };
        fgBom.Components.Add(new BillOfMaterialComponent
        {
            ComponentItemId = subAssyId,
            ComponentItemCode = "SUB-ASSY",
            RequiredQuantity = 2m
        });

        // BOM 2: Sub-Assembly requires 3 Raw Materials
        var subBom = new BillOfMaterialHeader
        {
            FinishedGoodItemId = subAssyId,
            BomCode = "BOM-SUB",
            BaseQuantity = 1m,
            IsActive = true
        };
        subBom.Components.Add(new BillOfMaterialComponent
        {
            ComponentItemId = rawMatId,
            ComponentItemCode = "RAW-STEEL",
            RequiredQuantity = 3m
        });

        var bomCatalog = new List<BillOfMaterialHeader> { fgBom, subBom };

        // Demand: Need 10 units of Finished Good
        var demands = new List<MrpDemandItem>
        {
            new()
            {
                ItemId = fgId,
                ItemCode = "FG-GEARBOX",
                GrossRequirementQty = 10m,
                RequirementDate = new DateOnly(2026, 12, 1),
                SourceReference = "SO-101"
            }
        };

        // Supplies: 2 FG in stock, 5 Sub-Assemblies in stock, 0 Raw Materials
        var supplies = new List<MrpSupplyItem>
        {
            new() { ItemId = fgId, OnHandQty = 2m, ScheduledReceiptsQty = 0m },
            new() { ItemId = subAssyId, OnHandQty = 5m, ScheduledReceiptsQty = 0m },
            new() { ItemId = rawMatId, OnHandQty = 0m, ScheduledReceiptsQty = 0m }
        };

        var plannedOrders = _mrpService.ExecuteMrpRun(
            demands,
            supplies,
            bomCatalog,
            isManufacturedItem: id => (id == fgId || id == subAssyId));

        // 1. FG net requirement: 10 - 2 = 8 units (Production)
        // 2. Sub-Assembly gross requirement: 8 FG * 2 = 16. Existing on hand = 5. Net requirement = 11 units (Production)
        // 3. Raw Material gross requirement: 11 Sub * 3 = 33. Existing on hand = 0. Net requirement = 33 units (Purchase)
        Assert.Equal(3, plannedOrders.Count);

        var fgOrder = plannedOrders.First(o => o.ItemId == fgId);
        Assert.Equal(8m, fgOrder.Quantity);
        Assert.Equal(PlannedOrderType.Production, fgOrder.OrderType);

        var subOrder = plannedOrders.First(o => o.ItemId == subAssyId);
        Assert.Equal(11m, subOrder.Quantity);
        Assert.Equal(PlannedOrderType.Production, subOrder.OrderType);

        var rawOrder = plannedOrders.First(o => o.ItemId == rawMatId);
        Assert.Equal(33m, rawOrder.Quantity);
        Assert.Equal(PlannedOrderType.Purchase, rawOrder.OrderType);
    }

    [Fact]
    public void DynamicQuery_FilteringSortingAndPaging_FunctionsAccurately()
    {
        var records = new List<PlannedOrder>
        {
            new() { OrderNumber = "PLN-001", Quantity = 100m, ItemCode = "ALPHA" },
            new() { OrderNumber = "PLN-002", Quantity = 250m, ItemCode = "BETA" },
            new() { OrderNumber = "PLN-003", Quantity = 50m, ItemCode = "ALPHA" },
            new() { OrderNumber = "PLN-004", Quantity = 300m, ItemCode = "GAMMA" },
            new() { OrderNumber = "PLN-005", Quantity = 80m, ItemCode = "ALPHA" }
        }.AsQueryable();

        var request = new DynamicQueryRequest
        {
            Filters = new List<QueryFilterCriteria>
            {
                new() { FieldName = "ItemCode", Operator = "eq", Value = "ALPHA" },
                new() { FieldName = "Quantity", Operator = "gt", Value = "60" }
            },
            SortBy = "Quantity",
            SortDescending = true,
            Skip = 0,
            Take = 10
        };

        var paged = _queryService.ExecuteQuery(records, request);

        // Alpha orders > 60: PLN-001 (100) and PLN-005 (80)
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count);
        Assert.Equal("PLN-001", paged.Items[0].OrderNumber);
        Assert.Equal("PLN-005", paged.Items[1].OrderNumber);
    }
}
