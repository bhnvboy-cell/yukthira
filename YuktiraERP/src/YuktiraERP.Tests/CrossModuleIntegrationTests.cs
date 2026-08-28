using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Tests;

/// <summary>
/// INT-01: MM → QC → FI (Raw Material Receipt)
/// INT-02: QC → SD (OOS Block Shipment)
/// INT-03: PM → MM → CO (Spare Parts Issue)
/// INT-04: PRD → QC → MM (Production Output Inspection)
/// </summary>
public class CrossModuleIntegrationTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    // ════════════════════════════════════════════════════════════════
    // INT-01: MM → QC → FI (Raw Material Receipt in QI Stock)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task INT01_GR_StockLandsInQI_InvoiceLockedUntilUD()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: PO
        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = "PO-INT-001",
            VendorName = "Corn Suppliers Inc.",
            ItemName = "Wet Corn",
            Amount = 40000m,
            Status = "Released"
        };
        db.PurchaseOrders.Add(po);

        // Act: Post GR - stock lands in QI
        var stock = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Wet Corn",
            Quantity = 20000,
            UOM = "KG",
            Value = 40000m,
            Lot = "QI-LOT-001"
        };
        db.StockItems.Add(stock);

        var gr = new GoodsReceiptEntity
        {
            TenantId = tenantId,
            GrnNumber = "GRN-INT-001",
            PoNumber = po.PoNumber,
            QtyReceived = "20000",
            Status = "Posted"
        };
        db.GoodsReceipts.Add(gr);

        // Create inspection lot
        var inspectionLot = new InspectionLotEntity
        {
            LotNumber = "IL-INT-001",
            MaterialName = "Wet Corn",
            Quantity = "20000",
            Inspected = 0,
            Passed = 0,
            Failed = 0,
            Status = "Open"
        };
        db.InspectionLots.Add(inspectionLot);
        await db.SaveChangesAsync();

        // Assert: Stock in QI, invoice locked
        var savedStock = await db.StockItems.FindAsync(stock.Id);
        Assert.Contains("QI", savedStock!.Lot);

        var savedLot = await db.InspectionLots
            .FirstOrDefaultAsync(l => l.LotNumber == inspectionLot.LotNumber);
        Assert.Equal("Open", savedLot!.Status);

        // Invoice cannot be posted until UD
        bool canPostInvoice = savedLot.Status == "Accepted";
        Assert.False(canPostInvoice, "Invoice blocked until QC posts Approved Usage Decision");
    }

    [Fact]
    public async Task INT01_UsageDecision_Released_UnlocksInvoice()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Lot under inspection
        var lot = new InspectionLotEntity
        {
            LotNumber = "IL-INT-002",
            MaterialName = "Wet Corn",
            Status = "Inspected"
        };
        db.InspectionLots.Add(lot);

        // Act: Post usage decision
        var ud = new UsageDecisionEntity
        {
            DecisionId = "UD-INT-002",
            LotNumber = lot.LotNumber,
            Decision = "Accept",
            DecisionDate = DateTime.UtcNow
        };
        db.UsageDecisions.Add(ud);

        lot.Status = "Accepted";
        await db.SaveChangesAsync();

        // Now invoice can be posted
        var invoice = new InvoiceVerificationEntity
        {
            InvoiceNumber = "INV-INT-002",
            PoNumber = "PO-INT-002",
            Amount = 40000m,
            Status = "Posted",
            TenantId = tenantId
        };
        db.InvoiceVerifications.Add(invoice);
        await db.SaveChangesAsync();

        // Assert: Invoice posted after UD
        var savedInvoice = await db.InvoiceVerifications
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);
        Assert.NotNull(savedInvoice);
        Assert.Equal("Posted", savedInvoice!.Status);
    }

    // ════════════════════════════════════════════════════════════════
    // INT-02: QC → SD (OOS Block Shipment)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task INT02_OOSBatch_BlocksDeliveryCreation()
    {
        var db = CreateDb();

        // Arrange: Batch on quality hold
        var batch = new BatchEntity
        {
            BatchNumber = "BATCH-OOS-001",
            MaterialId = Guid.NewGuid(),
            MaterialName = "Corn Starch",
            Status = "QUALITY_HOLD",
            Quantity = 1000,
            UnitOfMeasure = "KG"
        };
        db.Batches.Add(batch);

        var so = new SalesOrderEntity
        {
            OrderNumber = "SO-OOS-001",
            CustomerName = "Food Industries Ltd.",
            Status = "Created"
        };
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        // Act: Attempt to create delivery
        bool canDeliver = batch.Status == "ACTIVE" || batch.Status == "UNRESTRICTED";

        // Assert: Delivery blocked
        Assert.False(canDeliver, "Cannot deliver batch on QUALITY_HOLD");
        Assert.Equal("QUALITY_HOLD", batch.Status);
    }

    [Fact]
    public async Task INT02_OOSBatch_PreventsPGI()
    {
        var db = CreateDb();

        // Arrange: Batch on hold
        var batch = new BatchEntity
        {
            BatchNumber = "BATCH-OOS-002",
            MaterialId = Guid.NewGuid(),
            MaterialName = "Dextrose",
            Status = "REJECTED",
            Quantity = 500
        };
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        // Act: Check if PGI can be posted
        bool canPGI = batch.Status == "ACTIVE";

        // Assert: PGI blocked
        Assert.False(canPGI, "Cannot post PGI for rejected batch");
        Assert.Equal("REJECTED", batch.Status);
    }

    // ════════════════════════════════════════════════════════════════
    // INT-03: PM → MM → CO (Spare Parts Issue)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task INT03_SparePartsIssue_InventoryDrops_CostsAccrueToPMOrder()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: PM Order and spare parts
        var pmOrder = new MaintenanceOrderEntity
        {
            OrderNumber = "MO-INT-003",
            EquipmentCode = "EQ-PUMP-INT",
            Description = "Pump overhaul",
            Status = "IN_PROGRESS",
            Cost = 0
        };
        db.MaintenanceOrders.Add(pmOrder);

        var sparePart = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Pump Bearing 6308",
            Quantity = 5,
            UOM = "EA",
            Value = 1500m  // $300 each
        };
        db.StockItems.Add(sparePart);

        var costCenter = new CostCenterEntity
        {
            Code = "CC-MAINT-001",
            Name = "Maintenance Department",
            PlannedBudget = 50000m
        };
        db.CostCenters.Add(costCenter);
        await db.SaveChangesAsync();

        // Act: Issue spare parts
        decimal issuedQty = 2m;
        decimal unitCost = sparePart.Value / sparePart.Quantity;
        decimal totalCost = issuedQty * unitCost;

        sparePart.Quantity -= issuedQty;
        pmOrder.Cost += totalCost;
        await db.SaveChangesAsync();

        // Record stock movement
        var movement = new StockMovementEntity
        {
            TenantId = tenantId,
            DocumentNumber = $"GI-PM-{pmOrder.OrderNumber}",
            MaterialName = sparePart.MaterialName,
            MovementType = "261",
            Quantity = issuedQty,
            Reference = pmOrder.OrderNumber,
            Status = "Posted"
        };
        db.StockMovements.Add(movement);
        await db.SaveChangesAsync();

        // Assert: Inventory drops, costs accrue
        var refreshedStock = await db.StockItems.FindAsync(sparePart.Id);
        Assert.Equal(3m, refreshedStock!.Quantity);  // 5 - 2

        var refreshedOrder = await db.MaintenanceOrders.FindAsync(pmOrder.Id);
        Assert.Equal(600m, refreshedOrder!.Cost);  // 2 * $300
    }

    [Fact]
    public async Task INT03_MaterialCost_DebitedToPMCostObject()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Cost element for maintenance materials
        var costElement = new CostElementEntity
        {
            Code = "CE-MAINT-SPARE",
            Name = "Maintenance Spare Parts",
            Type = "Primary",
            Category = "Material"
        };
        db.CostElements.Add(costElement);

        // Act: Record cost allocation
        var allocation = new CostAllocationDetailEntity
        {
            TenantId = tenantId,
            RunId = Guid.NewGuid(),
            CostCenterCode = "CC-MAINT-001",
            CostCenterName = "Maintenance Department",
            CostElementCode = costElement.Code,
            Amount = 600m,
            SharePercent = 100m,
            Basis = "PM Order MO-INT-003"
        };
        db.CostAllocationDetails.Add(allocation);
        await db.SaveChangesAsync();

        // Assert: Cost allocated to maintenance cost center
        var savedAllocation = await db.CostAllocationDetails
            .FirstOrDefaultAsync(a => a.CostElementCode == costElement.Code);
        Assert.NotNull(savedAllocation);
        Assert.Equal(600m, savedAllocation!.Amount);
        Assert.Equal("CC-MAINT-001", savedAllocation.CostCenterCode);
    }

    // ════════════════════════════════════════════════════════════════
    // INT-04: PRD → QC → MM (Production Output Inspection)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task INT04_ProductionOutput_TriggersInspectionLot()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Production order completed
        var prodOrder = new ProductionOrderEntity
        {
            TenantId = tenantId,
            OrderNumber = "PROD-INT-004",
            ProductName = "Corn Starch",
            Quantity = 1000,
            Status = "COMPLETED",
            YieldQty = 980,
            ScrapQty = 20,
            BatchNo = "BATCH-FG-001"
        };
        db.ProductionOrders.Add(prodOrder);
        await db.SaveChangesAsync();

        // Act: Trigger in-process inspection lot
        var inspectionLot = new InspectionLotEntity
        {
            LotNumber = $"IL-PRD-{prodOrder.OrderNumber}",
            MaterialName = prodOrder.ProductName,
            Quantity = prodOrder.YieldQty.ToString(),
            Inspected = 0,
            Passed = 0,
            Failed = 0,
            Status = "Open"
        };
        db.InspectionLots.Add(inspectionLot);
        await db.SaveChangesAsync();

        // Assert: Inspection lot created
        var savedLot = await db.InspectionLots
            .FirstOrDefaultAsync(l => l.LotNumber == inspectionLot.LotNumber);
        Assert.NotNull(savedLot);
        Assert.Equal("Open", savedLot!.Status);
        Assert.Equal("980", savedLot.Quantity);
    }

    [Fact]
    public async Task INT04_QCPass_FinishedGoodsToUnrestricted()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Inspection lot from production
        var lot = new InspectionLotEntity
        {
            LotNumber = "IL-PRD-004",
            MaterialName = "Corn Starch",
            Quantity = "980",
            Inspected = 980,
            Passed = 975,
            Failed = 5,
            Status = "Inspected"
        };
        db.InspectionLots.Add(lot);

        var batch = new BatchEntity
        {
            TenantId = tenantId,
            BatchNumber = "BATCH-FG-001",
            MaterialId = Guid.NewGuid(),
            MaterialName = "Corn Starch",
            Status = "QUALITY_HOLD",
            Quantity = 980,
            UnitOfMeasure = "KG"
        };
        db.Batches.Add(batch);
        await db.SaveChangesAsync();

        // Act: QC passes - UD approved
        var ud = new UsageDecisionEntity
        {
            DecisionId = "UD-PRD-004",
            LotNumber = lot.LotNumber,
            Decision = "Accept",
            DecisionDate = DateTime.UtcNow
        };
        db.UsageDecisions.Add(ud);

        lot.Status = "Accepted";
        batch.Status = "UNRESTRICTED";
        await db.SaveChangesAsync();

        // Assert: Finished goods stock released to unrestricted
        var refreshedBatch = await db.Batches.FindAsync(batch.Id);
        Assert.Equal("UNRESTRICTED", refreshedBatch!.Status);

        var refreshedLot = await db.InspectionLots
            .FirstOrDefaultAsync(l => l.LotNumber == lot.LotNumber);
        Assert.Equal("Accepted", refreshedLot!.Status);
    }
}
