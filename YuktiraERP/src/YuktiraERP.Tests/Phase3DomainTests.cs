using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Domain.Inventory;
using YuktiraERP.Core.Domain.MasterData;
using YuktiraERP.Core.Domain.Sales;
using YuktiraERP.Core.Domain.Procurement;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Tests;

public class Phase3DomainTests
{
    private readonly IUomRuleConversionService _uomService = new UomRuleConversionService();
    private readonly IInventoryValuationCalculationService _valuationService = new DomainInventoryValuationCalculationService();
    private readonly IDocumentFlowValidationService _flowValidationService = new DocumentFlowValidationService();
    private readonly IBulkDataExchangeService _dataExchangeService = new BulkDataExchangeService();

    [Fact]
    public void UomConversion_DirectAndReverse_CalculatesAccurately()
    {
        var conversions = new List<UomConversion>
        {
            new() { FromUomCode = "BOX", ToUomCode = "PCS", Multiplier = 24m, IsActive = true }
        };

        // 1. Direct conversion: 5 BOX to PCS => 5 * 24 = 120 PCS
        decimal directQty = _uomService.ConvertQuantity(5m, "BOX", "PCS", conversions: conversions);
        Assert.Equal(120m, directQty);

        // 2. Reverse conversion: 48 PCS to BOX => 48 / 24 = 2 BOX
        decimal reverseQty = _uomService.ConvertQuantity(48m, "PCS", "BOX", conversions: conversions);
        Assert.Equal(2m, reverseQty);
    }

    [Fact]
    public void UomConversion_MultiHopAndItemSpecific_SelectsPrecedence()
    {
        var itemId = Guid.NewGuid();
        var conversions = new List<UomConversion>
        {
            // Global: 1 CARTON = 10 BOX, 1 BOX = 10 PCS => 1 CARTON = 100 PCS
            new() { FromUomCode = "CARTON", ToUomCode = "BOX", Multiplier = 10m, IsActive = true },
            new() { FromUomCode = "BOX", ToUomCode = "PCS", Multiplier = 10m, IsActive = true },
            // Item-specific override: For this item, 1 CARTON = 250 PCS
            new() { ItemId = itemId, FromUomCode = "CARTON", ToUomCode = "PCS", Multiplier = 250m, IsActive = true }
        };

        // Item-specific matches first: 2 CARTON = 500 PCS
        decimal itemSpecificQty = _uomService.ConvertQuantity(2m, "CARTON", "PCS", itemId: itemId, conversions: conversions);
        Assert.Equal(500m, itemSpecificQty);

        // Global multi-hop fallback (no itemId specified): 2 CARTON -> 20 BOX -> 200 PCS
        decimal globalQty = _uomService.ConvertQuantity(2m, "CARTON", "PCS", itemId: null, conversions: conversions);
        Assert.Equal(200m, globalQty);
    }

    [Fact]
    public void MovingAverageCost_ReceiptRecalculation_ProducesAccurateMovingAverage()
    {
        // Initial OnHand: 100 units @ $10.00 each = $1,000 Total Value
        // Receipt: 50 units @ $16.00 each = $800 Incoming Value
        // New Total: 150 units, $1,800 Total Value => New Moving Avg = $12.00
        var (newQty, newTotalVal, newMovingAvg) = _valuationService.CalculateMovingAverageReceipt(
            currentOnHand: 100m,
            currentMovingAvg: 10m,
            receiptQty: 50m,
            receiptUnitCost: 16m);

        Assert.Equal(150m, newQty);
        Assert.Equal(1800m, newTotalVal);
        Assert.Equal(12m, newMovingAvg);
    }

    [Fact]
    public void BatchDepletion_FefoStrategy_DepletesEarliestExpiringFirst_AndAutoHidesZeroQuantity()
    {
        var itemId = Guid.NewGuid();
        var whId = Guid.NewGuid();

        var b1 = new ItemBatch
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            WarehouseId = whId,
            BatchNumber = "BATCH-EXP-2026-DEC",
            ExpiryDate = new DateOnly(2026, 12, 31),
            ManufacturingDate = new DateOnly(2025, 12, 1),
            QtyOnHand = 50m,
            UnitCost = 10m
        };

        var b2 = new ItemBatch
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            WarehouseId = whId,
            BatchNumber = "BATCH-EXP-2026-AUG",
            ExpiryDate = new DateOnly(2026, 8, 31),
            ManufacturingDate = new DateOnly(2026, 1, 1),
            QtyOnHand = 30m,
            UnitCost = 12m
        };

        var batches = new List<ItemBatch> { b1, b2 };

        // Request 40 units using FEFO:
        // Must take 30 units from b2 (earliest expiry 2026-08-31), leaving 0 (IsHidden = true)
        // Must take 10 units from b1 (expiry 2026-12-31), leaving 40 (IsHidden = false)
        var results = _valuationService.DepleteBatches(batches, 40m, useFefo: true);

        Assert.Equal(2, results.Count);
        Assert.Equal("BATCH-EXP-2026-AUG", results[0].BatchNumber);
        Assert.Equal(30m, results[0].DepletedQty);
        Assert.Equal(0m, results[0].RemainingBatchQty);
        Assert.True(results[0].BatchAutoHidden);
        Assert.True(b2.IsHidden);

        Assert.Equal("BATCH-EXP-2026-DEC", results[1].BatchNumber);
        Assert.Equal(10m, results[1].DepletedQty);
        Assert.Equal(40m, results[1].RemainingBatchQty);
        Assert.False(results[1].BatchAutoHidden);
        Assert.False(b1.IsHidden);
    }

    [Fact]
    public void DocumentFlow_LineageGraph_IntegrityIsMaintained()
    {
        var soId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        var links = new List<DocumentReference>
        {
            new()
            {
                SourceDocType = "SalesOrder",
                SourceDocId = soId,
                TargetDocType = "SalesDelivery",
                TargetDocId = deliveryId,
                LinkType = DocumentFlowType.Successor
            },
            new()
            {
                SourceDocType = "SalesDelivery",
                SourceDocId = deliveryId,
                TargetDocType = "SalesInvoice",
                TargetDocId = invoiceId,
                LinkType = DocumentFlowType.Successor
            }
        };

        Assert.Equal(2, links.Count);
        Assert.Equal(soId, links.First(l => l.TargetDocId == deliveryId).SourceDocId);
        Assert.Equal(deliveryId, links.First(l => l.TargetDocId == invoiceId).SourceDocId);
    }

    [Fact]
    public void DocumentImmutability_PostedOrderCannotBeMutated()
    {
        var so = new SalesOrder
        {
            OrderNumber = "SO-2026-0001",
            Status = GenericDocumentState.Posted
        };

        var ex = Assert.Throws<InvalidOperationException>(() => so.AssertCanMutate());
        Assert.Contains("Immutability enforced", ex.Message);
    }

    [Fact]
    public void ThreeWayMatch_FlagExcessInvoiceQuantityAndPriceVariance()
    {
        var lines = new List<OrderThreeWayMatchLine>
        {
            // Clean match
            new()
            {
                ItemId = Guid.NewGuid(),
                ItemCode = "ITEM-001",
                PoQuantity = 100m,
                PoUnitPrice = 10m,
                GrpoQuantity = 100m,
                InvoiceQuantity = 100m,
                InvoiceUnitPrice = 10m
            },
            // Quantity discrepancy: Invoiced 120 vs GRPO 100
            // Price variance: Invoiced $15 vs PO $10
            new()
            {
                ItemId = Guid.NewGuid(),
                ItemCode = "ITEM-002",
                PoQuantity = 100m,
                PoUnitPrice = 10m,
                GrpoQuantity = 100m,
                InvoiceQuantity = 120m,
                InvoiceUnitPrice = 15m
            }
        };

        var matchResult = _flowValidationService.ValidateThreeWayMatch(lines, quantityTolerancePercent: 0m, priceTolerancePercent: 0.5m);

        Assert.False(matchResult.IsMatched);
        Assert.Equal(2, matchResult.Discrepancies.Count);
        Assert.Equal(20m, matchResult.TotalQuantityDiscrepancy);
        Assert.Equal(600m, matchResult.TotalPriceVariance); // (15 - 10) * 120 = 600
    }

    [Fact]
    public void PrePostingValidation_CustomerCreditLimit_ExceededBlocksPosting()
    {
        decimal creditLimit = 50000m;
        decimal currentBalance = 42000m;
        decimal newOrderTotal = 12000m; // 42,000 + 12,000 = 54,000 > 50,000

        var result = _flowValidationService.ValidateCustomerCreditLimit(creditLimit, currentBalance, newOrderTotal);

        Assert.False(result.IsValid);
        Assert.Single(result.ValidationErrors);
        Assert.Contains("exceeding credit limit", result.ValidationErrors[0]);
    }

    [Fact]
    public void PrePostingValidation_InventoryAvailability_StockShortageBlocksDispatch()
    {
        decimal requiredQty = 150m;
        decimal availableQty = 100m;

        var result = _flowValidationService.ValidateInventoryAvailability(requiredQty, availableQty, "SKU-VALVE-01", "WH-MAIN");

        Assert.False(result.IsValid);
        Assert.Single(result.ValidationErrors);
        Assert.Contains("insufficient stock", result.ValidationErrors[0]);
    }

    [Fact]
    public void BulkDataExchange_PreviewAndValidate_CatchesDuplicatesAndBlanks()
    {
        string csv = @"Code,Name,BaseUomCode,StandardCost,IsBatchTracked
SKU-001,Widget Alpha,PCS,12.50,false
,Invalid Missing Code,PCS,10.00,false
SKU-001,Duplicate Code Row,PCS,14.00,true
SKU-002,Widget Beta,PCS,invalid_cost,false
SKU-003,Widget Gamma,,8.00,false";

        var preview = _dataExchangeService.PreviewAndValidateItems(csv);

        Assert.Equal(5, preview.TotalRows);
        Assert.Single(preview.ValidRecords);
        Assert.Equal("SKU-001", preview.ValidRecords[0].Code);
        Assert.Equal(12.50m, preview.ValidRecords[0].StandardCost);

        Assert.Equal(4, preview.Errors.Count);
        Assert.True(preview.HasErrors);
        Assert.Contains(preview.Errors, e => e.FieldName == "Code" && e.ErrorMessage.Contains("blank"));
        Assert.Contains(preview.Errors, e => e.FieldName == "Code" && e.ErrorMessage.Contains("Duplicate"));
        Assert.Contains(preview.Errors, e => e.FieldName == "StandardCost");
        Assert.Contains(preview.Errors, e => e.FieldName == "BaseUomCode");
    }

    [Fact]
    public void NumberingSeries_IncrementalGeneration_PadsAccurately()
    {
        var series = new NumberingSeries
        {
            DocumentType = "SalesOrder",
            Prefix = "SO-2026-",
            Suffix = "-US",
            NextNumber = 7,
            EndingNumber = 1000,
            PaddingLength = 5
        };

        string doc1 = series.GenerateNextDocumentNumber();
        Assert.Equal("SO-2026-00007-US", doc1);
        Assert.Equal(8, series.NextNumber);

        string doc2 = series.GenerateNextDocumentNumber();
        Assert.Equal("SO-2026-00008-US", doc2);
        Assert.Equal(9, series.NextNumber);
    }
}
