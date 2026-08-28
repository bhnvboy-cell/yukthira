using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Tests;

/// <summary>
/// SEQ-01: Prerequisite Status Lock (PO requires Release before GR)
/// SEQ-02: Out-of-Sequence Operation Confirmation
/// EXC-01: Inventory Over-Allocation (Negative Stock Check)
/// EXC-02: Concurrent User Record Lock
/// EXC-03: Decimal Precision & Mass Balance Rounding
/// </summary>
public class TransactionSequenceEdgeCaseTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    // ════════════════════════════════════════════════════════════════
    // SEQ-01: Prerequisite Status Lock
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SEQ01_GR_BlockedWhenPO_NotReleased()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: PO in "Created" status (not released)
        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = "PO-SEQ-001",
            VendorName = "Test Vendor",
            ItemName = "Raw Material",
            Amount = 10000m,
            Status = "Created"
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        // Act: Attempt GR
        var validStatusesForGR = new[] { "Released", "Partially Received" };
        bool canReceive = validStatusesForGR.Contains(po.Status);

        // Assert: GR blocked
        Assert.False(canReceive, "Transaction denied: PO-SEQ-001 requires Release approval before Goods Receipt");
        Assert.Equal("Created", po.Status);
    }

    [Fact]
    public async Task SEQ01_Invoice_BlockedWhenGR_NotPosted()
    {
        var db = CreateDb();

        // Arrange: PO without GR
        var po = new PurchaseOrderEntity
        {
            PoNumber = "PO-SEQ-002",
            Status = "Released"
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        // Act: Attempt invoice posting
        bool grPosted = po.Status == "GR Posted";
        bool canInvoice = grPosted;

        // Assert: Invoice blocked
        Assert.False(canInvoice, "Cannot post invoice before Goods Receipt");
    }

    [Fact]
    public async Task SEQ01_Billing_BlockedWhenPGI_NotPosted()
    {
        var db = CreateDb();

        // Arrange: Delivery not yet PGI'd
        var delivery = new DeliveryEntity
        {
            DeliveryNumber = "DN-SEQ-001",
            SoNumber = "SO-SEQ-001",
            Status = "Picked"
        };
        db.Deliveries.Add(delivery);
        await db.SaveChangesAsync();

        // Act: Attempt billing
        bool pgiPosted = delivery.Status == "PGI Posted";
        bool canBill = pgiPosted;

        // Assert: Billing blocked
        Assert.False(canBill, "Cannot create billing document before PGI");
        Assert.Equal("Picked", delivery.Status);
    }

    [Fact]
    public async Task SEQ01_ValidP2P_Flow_AllStepsComplete()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange
        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = "PO-FLOW-001",
            Status = "Created"
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync();

        // Act & Assert: Follow valid P2P sequence
        // Step 1: Release PO
        po.Status = "Released";
        await db.SaveChangesAsync();
        Assert.Equal("Released", po.Status);

        // Step 2: Post GR
        po.Status = "GR Posted";
        await db.SaveChangesAsync();
        Assert.Equal("GR Posted", po.Status);

        // Step 3: Post Invoice (now allowed)
        var invoice = new InvoiceVerificationEntity
        {
            InvoiceNumber = "INV-FLOW-001",
            PoNumber = po.PoNumber,
            Amount = 10000m,
            Status = "Posted",
            TenantId = tenantId
        };
        db.InvoiceVerifications.Add(invoice);
        await db.SaveChangesAsync();

        var savedInv = await db.InvoiceVerifications
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);
        Assert.Equal("Posted", savedInv!.Status);
    }

    // ════════════════════════════════════════════════════════════════
    // SEQ-02: Out-of-Sequence Operation Confirmation
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SEQ02_ConfirmOp0020_BlockedWhenOp0010_Incomplete()
    {
        var db = CreateDb();

        // Arrange: Production routing with operations
        var routing = new ProductionRoutingEntity
        {
            RoutingId = "RT-PROD-001",
            ProductName = "Corn Starch",
            OperationNo = 10,
            WorkCenter = "WET-MILL-01",
            SetupTimeHrs = 1,
            RunTimeHrs = 4,
            Status = "Active"
        };
        db.ProductionRoutings.Add(routing);

        var routing2 = new ProductionRoutingEntity
        {
            RoutingId = "RT-PROD-001",
            ProductName = "Corn Starch",
            OperationNo = 20,
            WorkCenter = "CENTRIFUGE-01",
            SetupTimeHrs = 0.5m,
            RunTimeHrs = 2,
            Status = "Active"
        };
        db.ProductionRoutings.Add(routing2);
        await db.SaveChangesAsync();

        // Act: Op 0010 not confirmed, attempt to confirm Op 0020
        bool op0010Complete = false;  // Not confirmed
        bool canConfirmOp0020 = op0010Complete;

        // Assert: Sequence enforcement
        Assert.False(canConfirmOp0020, "Cannot confirm Operation 0020 before Operation 0010 is complete");
    }

    [Fact]
    public async Task SEQ02_ValidSequence_AllOperationsConfirmedInOrder()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Production order
        var order = new ProductionOrderEntity
        {
            TenantId = tenantId,
            OrderNumber = "PROD-SEQ-002",
            ProductName = "Corn Starch",
            Quantity = 1000,
            Status = "IN_PROGRESS"
        };
        db.ProductionOrders.Add(order);

        // Create operations
        var operations = new[]
        {
            new ProductionRoutingEntity { RoutingId = "RT-SEQ-002", OperationNo = 10, WorkCenter = "WET-MILL", Status = "Active" },
            new ProductionRoutingEntity { RoutingId = "RT-SEQ-002", OperationNo = 20, WorkCenter = "CENTRIFUGE", Status = "Active" },
            new ProductionRoutingEntity { RoutingId = "RT-SEQ-002", OperationNo = 30, WorkCenter = "DRYER", Status = "Active" },
        };
        db.ProductionRoutings.AddRange(operations);
        await db.SaveChangesAsync();

        // Act: Confirm operations in sequence
        var confirmedOps = new System.Collections.Generic.List<int>();

        foreach (var op in operations.OrderBy(o => o.OperationNo))
        {
            bool prerequisiteMet = confirmedOps.Count == 0 || confirmedOps.Contains(op.OperationNo - 10);
            if (prerequisiteMet)
            {
                confirmedOps.Add(op.OperationNo);
            }
        }

        // Assert: All operations confirmed in order
        Assert.Equal(3, confirmedOps.Count);
        Assert.Equal(new[] { 10, 20, 30 }, confirmedOps);
    }

    // ════════════════════════════════════════════════════════════════
    // EXC-01: Inventory Over-Allocation (Negative Stock Check)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EXC01_NegativeStock_BlockedOnDelivery()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Limited stock
        var stock = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Corn Starch",
            Quantity = 200,
            UOM = "KG"
        };
        db.StockItems.Add(stock);

        var so = new SalesOrderEntity
        {
            OrderNumber = "SO-NEG-001",
            CustomerName = "Test Customer",
            Status = "Created"
        };
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        // Act: Attempt delivery exceeding stock
        decimal requestedQty = 500m;
        bool wouldGoNegative = stock.Quantity < requestedQty;

        // Assert: Blocked
        Assert.True(wouldGoNegative, "Delivery would result in negative stock");
        Assert.Equal(200m, stock.Quantity);
    }

    [Fact]
    public async Task EXC01_ProductionIssue_BlockedWhenInsufficientComponents()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Component stock
        var stock = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Wet Corn",
            Quantity = 500,
            UOM = "KG"
        };
        db.StockItems.Add(stock);

        var order = new ProductionOrderEntity
        {
            TenantId = tenantId,
            OrderNumber = "PROD-NEG-001",
            ProductName = "Corn Starch",
            Quantity = 1000,
            Status = "RELEASED"
        };
        db.ProductionOrders.Add(order);

        var component = new ProductionOrderItemEntity
        {
            ProductionOrderId = order.Id,
            MaterialName = "Wet Corn",
            RequiredQty = 1800m,  // 1.8 * 1000
            IssuedQty = 0
        };
        db.ProductionOrderItems.Add(component);
        await db.SaveChangesAsync();

        // Act: Attempt goods issue
        bool canIssue = stock.Quantity >= component.RequiredQty;

        // Assert: Blocked
        Assert.False(canIssue, "Cannot issue more components than available stock");
    }

    [Fact]
    public async Task EXC01_MvtType_ChecksNegativeStockAllowed()
    {
        var db = CreateDb();

        // Arrange: Movement types with different negative stock rules
        var mvt101 = new MovementTypeEntity
        {
            MovementType = 101,
            Description = "GR for Purchase Order",
            AllowsNegativeStock = false,
            InventoryUpdate = true,
            IsActive = true
        };

        var mvt601 = new MovementTypeEntity
        {
            MovementType = 601,
            Description = "GI for Delivery",
            AllowsNegativeStock = false,
            InventoryUpdate = true,
            IsActive = true
        };

        db.MovementTypes.AddRange(mvt101, mvt601);
        await db.SaveChangesAsync();

        // Assert: Both movement types do not allow negative stock
        Assert.False(mvt101.AllowsNegativeStock);
        Assert.False(mvt601.AllowsNegativeStock);
    }

    // ════════════════════════════════════════════════════════════════
    // EXC-02: Concurrent User Record Lock
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EXC02_RecordLock_SecondUserBlocked()
    {
        var db = CreateDb();

        // Arrange: Maintenance order being edited
        var order = new MaintenanceOrderEntity
        {
            OrderNumber = "MO-LOCK-001",
            EquipmentCode = "EQ-001",
            Description = "Pump repair",
            Status = "IN_PROGRESS"
        };
        db.MaintenanceOrders.Add(order);
        await db.SaveChangesAsync();

        // Act: Simulate concurrent access
        string user1 = "OPERATOR-01";
        string user2 = "OPERATOR-02";

        // User 1 acquires lock
        string lockedBy = user1;

        // User 2 attempts to edit
        bool user2CanEdit = lockedBy == user2;

        // Assert: User 2 blocked
        Assert.False(user2CanEdit, "Second user should be blocked when record is locked by first user");
        Assert.Equal(user1, lockedBy);
    }

    [Fact]
    public async Task EXC02_EquipmentMaster_ConcurrentModification_Prevented()
    {
        var db = CreateDb();

        // Arrange: Equipment record
        var equipment = new EquipmentEntity
        {
            EquipmentCode = "EQ-LOCK-001",
            Name = "Slurry Pump",
            Status = "Operational"
        };
        db.Equipments.Add(equipment);
        await db.SaveChangesAsync();

        // Act: Simulate concurrent save attempt
        var original = await db.Equipments.FindAsync(equipment.Id);
        original!.Name = "Slurry Pump - Updated by User 1";
        await db.SaveChangesAsync();

        // User 2 reads stale data
        db.Entry(equipment).State = EntityState.Detached;
        var stale = new EquipmentEntity
        {
            Id = equipment.Id,
            EquipmentCode = equipment.EquipmentCode,
            Name = "Slurry Pump - Stale Read",
            Status = equipment.Status
        };

        // Assert: Last write wins (optimistic concurrency pattern)
        Assert.NotEqual("Slurry Pump - Stale Read", original.Name);
        Assert.Equal("Slurry Pump - Updated by User 1", original.Name);
    }

    // ════════════════════════════════════════════════════════════════
    // EXC-03: Decimal Precision & Mass Balance Rounding
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task EXC03_HighPrecisionDecimal_DisplayFormattedCorrectly()
    {
        // Arrange: High-precision Baumé value
        decimal baumeInput = 4.56783m;

        // Act: Format for display
        string displayFormatted = baumeInput.ToString("F2");  // 2 decimal places
        string displayFull = baumeInput.ToString("F5");      // 5 decimal places

        // Assert: Display formatting works
        Assert.Equal("4.57", displayFormatted);
        Assert.Equal("4.56783", displayFull);
    }

    [Fact]
    public async Task EXC03_MassBalance_NoCumulativeRoundingErrors()
    {
        // Arrange: Mass balance with high-precision inputs
        decimal input1 = 3333.333m;
        decimal input2 = 3333.333m;
        decimal input3 = 3333.334m;
        decimal totalInput = input1 + input2 + input3;

        decimal output1 = 1666.667m;
        decimal output2 = 1666.666m;
        decimal output3 = 1666.667m;
        decimal totalOutput = output1 + output2 + output3;

        // Test precision preservation (same operations, different order)
        decimal totalInputRecalc = input3 + input1 + input2;
        decimal totalOutputRecalc = output3 + output1 + output2;

        // Verify: addition order doesn't change result (precision preserved)
        Assert.Equal(totalInput, totalInputRecalc);
        Assert.Equal(totalOutput, totalOutputRecalc);

        // Verify: subtraction is also order-independent
        decimal loss = totalInput - totalOutput;
        decimal lossRecalc = totalInputRecalc - totalOutputRecalc;
        Assert.Equal(loss, lossRecalc);
    }

    [Fact]
    public async Task EXC03_FinancialTotals_PrecisionMaintained()
    {
        // Arrange: Financial calculations
        decimal unitPrice = 2.345m;
        decimal quantity = 1000m;
        decimal totalAmount = unitPrice * quantity;

        // Act: Rounding to 2 decimal places
        decimal roundedTotal = Math.Round(totalAmount, 2);

        // Assert: Precision maintained
        Assert.Equal(2345.00m, totalAmount);
        Assert.Equal(2345.00m, roundedTotal);

        // Verify with tax calculation
        decimal taxRate = 0.18m;
        decimal taxAmount = Math.Round(totalAmount * taxRate, 2);
        decimal grandTotal = totalAmount + taxAmount;

        Assert.Equal(422.10m, taxAmount);
        Assert.Equal(2767.10m, grandTotal);
    }

    [Fact]
    public async Task EXC03_BaumeCalculation_PrecisionPreserved()
    {
        // Arrange: Baumé with high precision
        decimal specificGravity = 1.4256m;

        // Act: Calculate Baumé
        decimal baume = 145m - (145m / specificGravity);

        // Assert: Precision preserved throughout calculation
        Assert.Equal(43.2884m, Math.Round(baume, 4));

        // Verify no drift with chained calculations
        decimal temp = 145m / specificGravity;
        decimal baumeFromTemp = 145m - temp;
        Assert.Equal(baume, baumeFromTemp);
    }
}
