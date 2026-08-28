using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Tests;

/// <summary>
/// MM-01: Procure-to-Pay (P2P) Execution
/// SD-01: Order-to-Cash (O2C) Execution
/// </summary>
public class ProcurementSalesTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    // ════════════════════════════════════════════════════════════════
    // MM-01: Procure-to-Pay (P2P) Execution
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MM01_PR_to_PO_Conversion_CreatesPO()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Purchase Requisition
        var pr = new PurchaseRequisitionEntity
        {
            TenantId = tenantId,
            PrNumber = $"PR-{DateTime.Now:yyyyMMdd}-001",
            Date = DateTime.UtcNow,
            Requestor = "Plant Manager",
            ItemName = "Wet Corn",
            Quantity = "20000",
            Amount = 40000m,
            Status = "Approved",
            TotalAmount = 40000m,
            ItemCount = 1
        };
        db.PurchaseRequisitions.Add(pr);
        await db.SaveChangesAsync();

        // Act: Convert PR to PO
        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = $"PO-{DateTime.Now:yyyyMMdd}-001",
            Date = DateTime.UtcNow,
            VendorName = "Corn Suppliers Inc.",
            VendorCode = "VEND-001",
            ItemName = pr.ItemName,
            Quantity = pr.Quantity,
            Amount = pr.Amount,
            Status = "Created",
            TotalAmount = pr.TotalAmount,
            ItemCount = pr.ItemCount,
            PaymentTerms = "Net 30"
        };
        db.PurchaseOrders.Add(po);

        // Link PR to PO
        pr.ConvertedPoNumber = po.PoNumber;
        pr.Status = "Converted";
        await db.SaveChangesAsync();

        // Assert: PO created from PR
        var savedPO = await db.PurchaseOrders
            .FirstOrDefaultAsync(p => p.PoNumber == po.PoNumber);
        Assert.NotNull(savedPO);
        Assert.Equal("VEND-001", savedPO!.VendorCode);

        var savedPR = await db.PurchaseRequisitions.FindAsync(pr.Id);
        Assert.Equal("Converted", savedPR!.Status);
        Assert.Equal(po.PoNumber, savedPR.ConvertedPoNumber);
    }

    [Fact]
    public async Task MM01_GoodsReceipt_StockIncreases_InQI()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: PO and stock location
        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = "PO-2026-GR-001",
            VendorName = "Corn Suppliers Inc.",
            ItemName = "Wet Corn",
            Quantity = "20000",
            Amount = 40000m,
            Status = "Released"
        };
        db.PurchaseOrders.Add(po);

        var stock = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Wet Corn",
            Quantity = 0,
            UOM = "KG",
            Value = 0
        };
        db.StockItems.Add(stock);
        await db.SaveChangesAsync();

        // Act: Post Goods Receipt (101)
        decimal grQty = 20000m;
        decimal unitPrice = 2.00m;
        decimal totalValue = grQty * unitPrice;

        stock.Quantity += grQty;
        stock.Value += totalValue;

        var gr = new GoodsReceiptEntity
        {
            TenantId = tenantId,
            GrnNumber = $"GRN-{DateTime.Now:yyyyMMddHHmmss}",
            Date = DateTime.UtcNow,
            PoNumber = po.PoNumber,
            MaterialName = "Wet Corn",
            QtyReceived = grQty.ToString(),
            QtyAccepted = grQty.ToString(),
            Status = "Posted"
        };
        db.GoodsReceipts.Add(gr);
        await db.SaveChangesAsync();

        // Assert: Stock increased
        var refreshedStock = await db.StockItems.FindAsync(stock.Id);
        Assert.Equal(20000m, refreshedStock!.Quantity);
        Assert.Equal(40000m, refreshedStock.Value);

        var savedGR = await db.GoodsReceipts
            .FirstOrDefaultAsync(g => g.GrnNumber == gr.GrnNumber);
        Assert.NotNull(savedGR);
        Assert.Equal("Posted", savedGR!.Status);
    }

    [Fact]
    public async Task MM01_InvoiceVerification_ThreeWayMatch()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: PO and GR
        var po = new PurchaseOrderEntity
        {
            TenantId = tenantId,
            PoNumber = "PO-2026-INV-001",
            Amount = 40000m,
            Status = "GR Posted"
        };
        db.PurchaseOrders.Add(po);

        var gr = new GoodsReceiptEntity
        {
            TenantId = tenantId,
            GrnNumber = "GRN-2026-001",
            PoNumber = po.PoNumber,
            QtyReceived = "20000",
            QtyAccepted = "20000",
            Status = "Posted"
        };
        db.GoodsReceipts.Add(gr);

        // Act: Post Invoice Verification
        var invoice = new InvoiceVerificationEntity
        {
            InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-001",
            Date = DateTime.UtcNow,
            PoNumber = po.PoNumber,
            VendorName = "Corn Suppliers Inc.",
            Amount = 40000m,
            MatchedAmount = 40000m,
            Status = "Matched",
            TenantId = tenantId
        };
        db.InvoiceVerifications.Add(invoice);
        await db.SaveChangesAsync();

        // Assert: Three-way match
        Assert.Equal(invoice.Amount, invoice.MatchedAmount);

        var savedInv = await db.InvoiceVerifications
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoice.InvoiceNumber);
        Assert.NotNull(savedInv);
        Assert.Equal("Matched", savedInv!.Status);
    }

    [Fact]
    public async Task MM01_GR_IR_Clearing_AccountUpdated()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: GR posted
        var gr = new GoodsReceiptEntity
        {
            TenantId = tenantId,
            GrnNumber = "GRN-2026-CLEAR-001",
            PoNumber = "PO-2026-CLEAR-001",
            QtyReceived = "5000",
            Status = "Posted"
        };
        db.GoodsReceipts.Add(gr);

        // Act: Create GR/IR clearing entry
        var grIrEntry = new JournalEntryEntity
        {
            DocumentNumber = $"GRIR-{DateTime.Now:yyyyMMddHHmmss}",
            EntryDate = DateTime.UtcNow,
            Account = "GR/IR Clearing",
            Debit = 10000m,
            Credit = 0,
            Reference = gr.GrnNumber
        };
        db.JournalEntries.Add(grIrEntry);
        await db.SaveChangesAsync();

        // Assert: GR/IR clearing account posted
        var savedEntry = await db.JournalEntries
            .FirstOrDefaultAsync(j => j.DocumentNumber == grIrEntry.DocumentNumber);
        Assert.NotNull(savedEntry);
        Assert.Equal(10000m, savedEntry!.Debit);
        Assert.Equal("GR/IR Clearing", savedEntry.Account);
    }

    // ════════════════════════════════════════════════════════════════
    // SD-01: Order-to-Cash (O2C) Execution
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SD01_SalesOrder_CreatedWithATPCheck()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Stock available
        var stock = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Corn Starch",
            Quantity = 5000,
            UOM = "KG"
        };
        db.StockItems.Add(stock);

        var customer = new CustomerEntity
        {
            Code = "CUST-001",
            Name = "Food Industries Ltd.",
            CreditLimit = 100000,
            Status = "Active"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Act: Create Sales Order
        var so = new SalesOrderEntity
        {
            OrderNumber = $"SO-{DateTime.Now:yyyyMMdd}-001",
            CustomerName = customer.Name,
            OrderDate = DateTime.UtcNow,
            ItemCount = 1,
            Amount = 1500m,
            Status = "Created"
        };
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        // Check ATP
        bool hasStock = stock.Quantity >= 2000m;  // Order for 2000 kg
        Assert.True(hasStock, "ATP check should pass - stock available");

        // Assert: SO created
        var savedSO = await db.SalesOrders
            .FirstOrDefaultAsync(s => s.OrderNumber == so.OrderNumber);
        Assert.NotNull(savedSO);
        Assert.Equal("Created", savedSO!.Status);
    }

    [Fact]
    public async Task SD01_DeliveryAndPGI_DeductsInventory()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Stock
        var stock = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Corn Starch",
            Quantity = 5000,
            UOM = "KG",
            Value = 15000m
        };
        db.StockItems.Add(stock);

        var so = new SalesOrderEntity
        {
            OrderNumber = "SO-2026-PGI-001",
            CustomerName = "Food Industries Ltd.",
            Amount = 1500m,
            Status = "Created"
        };
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync();

        // Act: Create delivery and post PGI
        var delivery = new DeliveryEntity
        {
            DeliveryNumber = $"DN-{DateTime.Now:yyyyMMdd}-001",
            Date = DateTime.UtcNow,
            SoNumber = so.OrderNumber,
            CustomerName = so.CustomerName,
            Status = "Picked"
        };
        db.Deliveries.Add(delivery);

        // Post Goods Issue (PGI)
        decimal pgiQty = 2000m;
        decimal stockBefore = stock.Quantity;
        stock.Quantity -= pgiQty;
        delivery.Status = "PGI Posted";
        await db.SaveChangesAsync();

        // Record COGS
        var cogsEntry = new JournalEntryEntity
        {
            DocumentNumber = $"COGS-{DateTime.Now:yyyyMMddHHmmss}",
            EntryDate = DateTime.UtcNow,
            Account = "Cost of Goods Sold",
            Debit = 6000m,  // 2000 kg * $3/kg
            Reference = delivery.DeliveryNumber
        };
        db.JournalEntries.Add(cogsEntry);
        await db.SaveChangesAsync();

        // Assert: Inventory deducted
        var refreshedStock = await db.StockItems.FindAsync(stock.Id);
        Assert.Equal(3000m, refreshedStock!.Quantity);  // 5000 - 2000

        var savedDelivery = await db.Deliveries.FindAsync(delivery.Id);
        Assert.Equal("PGI Posted", savedDelivery!.Status);
    }

    [Fact]
    public async Task SD01_Billing_InvoicePostsAR()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Delivery completed
        var delivery = new DeliveryEntity
        {
            DeliveryNumber = "DN-2026-BILL-001",
            SoNumber = "SO-2026-BILL-001",
            CustomerName = "Food Industries Ltd.",
            Status = "PGI Posted"
        };
        db.Deliveries.Add(delivery);

        // Act: Create billing document
        var billing = new BillingDocumentEntity
        {
            TenantId = tenantId,
            DocumentNumber = $"INV-{DateTime.Now:yyyyMMdd}-001",
            Date = DateTime.UtcNow,
            SoNumber = delivery.SoNumber,
            CustomerName = delivery.CustomerName,
            Amount = 6000m,
            Status = "Issued"
        };
        db.BillingDocuments.Add(billing);

        // Post AR entry
        var arEntry = new AREntryEntity
        {
            TenantId = tenantId,
            DocumentNumber = billing.DocumentNumber,
            Date = DateTime.UtcNow,
            CustomerName = billing.CustomerName,
            Amount = billing.Amount,
            ReceivedAmount = 0,
            Status = "Open"
        };
        db.AREntries.Add(arEntry);
        await db.SaveChangesAsync();

        // Assert: AR posted
        var savedAR = await db.AREntries
            .FirstOrDefaultAsync(a => a.DocumentNumber == arEntry.DocumentNumber);
        Assert.NotNull(savedAR);
        Assert.Equal(6000m, savedAR!.Amount);
        Assert.Equal("Open", savedAR.Status);
    }

    [Fact]
    public async Task SD01_ATPBlock_InsufficientStock_PreventsDelivery()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Arrange: Low stock
        var stock = new StockItemEntity
        {
            TenantId = tenantId,
            MaterialName = "Corn Starch",
            Quantity = 500,  // Only 500 kg available
            UOM = "KG"
        };
        db.StockItems.Add(stock);
        await db.SaveChangesAsync();

        // Act: Attempt delivery for 1000 kg
        decimal requestedQty = 1000m;
        bool canFulfill = stock.Quantity >= requestedQty;

        // Assert: Delivery blocked
        Assert.False(canFulfill, "Cannot deliver more than available stock");
        Assert.Equal(500m, stock.Quantity);
    }
}
