using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Infrastructure.Services;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Tests;

/// <summary>
/// Customer Complaint & Return with Supplier Pass-Through Claim (SD-QM-MM-FI)
/// CR-01 to CR-08: End-to-End Cross-Functional Workflow Tests
/// </summary>
public class CustomerComplaintReturnTests
{
    private YuktiraDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<YuktiraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new YuktiraDbContext(options);
    }

    private ICustomerComplaintReturnService CreateService(YuktiraDbContext db) =>
        new CustomerComplaintReturnService(db);

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-01: Customer Complaint & Return Order Creation
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CR01_CreateComplaintAndReturnOrder_Success()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var request = new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            CustomerName = "Acme Foods Ltd",
            MaterialCode = "MAT-CORN-STARCH",
            MaterialName = "Corn Starch",
            ReturnQuantity = 500,
            UOM = "KG",
            UnitPrice = 45.00m,
            BatchNumber = "BATCH-2026-001",
            DefectCode = "RAW-IMPURITY",
            DefectDescription = "Raw material impurity detected in incoming batch",
            DefectCategory = "SUPPLIER",
            SupplierVendorCode = "VEND-001",
            SupplierVendorName = "Global Starch Corp",
            SupplierBatchNumber = "SUP-BATCH-789",
            PurchaseOrderReference = "PO-2026-001",
            Plant = "PLT-01",
            StorageLocation = "SL-01"
        };

        var result = await service.CreateComplaintAndReturnOrderAsync(request);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.ComplaintReturnId);
        Assert.StartsWith("CR-", result.ComplaintNumber);
        Assert.StartsWith("RE-", result.ReturnOrderNumber);
        Assert.StartsWith("QN-", result.QualityNotificationNumber);
        Assert.Equal(22500m, result.ReturnAmount); // 500 * 45
        Assert.Equal("CREATED", result.Status);
        Assert.Equal("CR-01", result.CurrentStep);
    }

    [Fact]
    public async Task CR01_CreateComplaint_CreatesWorkflowSteps()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var request = new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-002",
            CustomerName = "Test Corp",
            MaterialCode = "MAT-001",
            MaterialName = "Test Material",
            ReturnQuantity = 100,
            UnitPrice = 10.00m
        };

        var result = await service.CreateComplaintAndReturnOrderAsync(request);

        var steps = await db.ComplaintWorkflowSteps
            .Where(s => s.ComplaintReturnId == result.ComplaintReturnId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        Assert.Equal(8, steps.Count);
        Assert.Equal("CR-01", steps[0].StepCode);
        Assert.Equal("CR-08", steps[7].StepCode);
        Assert.Equal("COMPLETED", steps[0].Status);
        Assert.All(steps.Skip(1), s => Assert.Equal("PENDING", s.Status));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-02: Return Delivery & Goods Receipt (Mvt 651)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CR02_PostReturnDelivery_MovementType651_QIStock()
    {
        var db = CreateDb();
        var service = CreateService(db);

        // Create complaint first
        var complaint = await service.CreateComplaintAndReturnOrderAsync(new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            MaterialCode = "MAT-001",
            ReturnQuantity = 200,
            UnitPrice = 50.00m
        });

        var result = await service.PostReturnDeliveryAsync(new ReturnDeliveryRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            DeliveryNumber = "DEL-001",
            Quantity = 200,
            BatchNumber = "BATCH-001",
            Plant = "PLT-01",
            StorageLocation = "SL-01"
        });

        Assert.True(result.Success);
        Assert.Equal(651, result.MovementType);
        Assert.Equal("QI", result.StockType);
        Assert.StartsWith("MD-", result.MaterialDocumentNumber);
        Assert.StartsWith("IL-", result.InspectionLotNumber);

        // Verify stock movement was created
        var stockMovement = await db.StockMovements
            .FirstOrDefaultAsync(s => s.Reference == complaint.ComplaintNumber);
        Assert.NotNull(stockMovement);
        Assert.Equal("651", stockMovement.MovementType);
        Assert.Equal(200, stockMovement.Quantity);

        // Verify complaint updated
        var updatedComplaint = await db.CustomerComplaintReturns.FindAsync(complaint.ComplaintReturnId);
        Assert.Equal("RETURN_RECEIVED", updatedComplaint.Status);
        Assert.NotNull(updatedComplaint.InspectionLotNumber);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-03: Quality Inspection Root-Cause Analysis
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CR03_RecordInspection_SupplierDefect_Detected()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var complaint = await service.CreateComplaintAndReturnOrderAsync(new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            MaterialCode = "MAT-001",
            SupplierVendorCode = "VEND-001",
            SupplierBatchNumber = "SUP-001"
        });

        await service.PostReturnDeliveryAsync(new ReturnDeliveryRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            Quantity = 100
        });

        var result = await service.RecordInspectionResultsAsync(new QualityInspectionRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            InspectionLotNumber = "IL-001",
            Characteristic = "Purity",
            Specification = "Min 99.5%",
            ResultValue = "98.2",
            ResultValuation = "NOK",
            DefectCodeGroup = "MATERIAL",
            DefectCode = "RAW-IMPURITY",
            DefectDescription = "Foreign particles detected in raw material batch",
            DefectCategory = "SUPPLIER",
            RootCause = "Supplier raw material contamination - vendor batch SUP-001",
            RootCauseCode = "SUPPLIER-RAW",
            RecordedBy = "LAB-TECH-01"
        });

        Assert.True(result.Success);
        Assert.True(result.IsSupplierDefect);
        Assert.Contains("supplier", result.RootCause, StringComparison.OrdinalIgnoreCase);

        // Verify complaint updated
        var updatedComplaint = await db.CustomerComplaintReturns.FindAsync(complaint.ComplaintReturnId);
        Assert.Equal("INSPECTION_COMPLETED", updatedComplaint.Status);
        Assert.Equal("Supplier raw material contamination - vendor batch SUP-001", updatedComplaint.RootCause);
    }

    [Fact]
    public async Task CR03_RecordInspection_InternalDefect_NoSupplierClaim()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var complaint = await service.CreateComplaintAndReturnOrderAsync(new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            MaterialCode = "MAT-001"
        });

        var result = await service.RecordInspectionResultsAsync(new QualityInspectionRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            RootCause = "Internal process issue during production",
            RootCauseCode = "INTERNAL-PROCESS"
        });

        Assert.True(result.Success);
        Assert.False(result.IsSupplierDefect);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-04: Usage Decision (Reject → Blocked Stock)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CR04_PostUsageDecision_Reject_MoveToBlockedStock()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var complaint = await service.CreateComplaintAndReturnOrderAsync(new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            MaterialCode = "MAT-001",
            ReturnQuantity = 150,
            UnitPrice = 30.00m
        });

        await service.PostReturnDeliveryAsync(new ReturnDeliveryRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            Quantity = 150
        });

        await service.RecordInspectionResultsAsync(new QualityInspectionRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            RootCauseCode = "SUPPLIER-RAW"
        });

        var result = await service.PostUsageDecisionAsync(new UsageDecisionRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            UsageDecision = "R",
            UsageDecisionCode = "R",
            StockProposal = "349",
            TargetStockType = "BLOCKED",
            DecidedBy = "QM-MANAGER-01"
        });

        Assert.True(result.Success);
        Assert.Equal("R", result.UsageDecision);
        Assert.Equal("BLOCKED", result.StockType);

        // Verify stock movement created
        var stockMovement = await db.StockMovements
            .FirstOrDefaultAsync(s => s.MovementType == "349");
        Assert.NotNull(stockMovement);
        Assert.Equal(150, stockMovement.Quantity);

        // Verify complaint updated
        var updatedComplaint = await db.CustomerComplaintReturns.FindAsync(complaint.ComplaintReturnId);
        Assert.Equal("USAGE_DECIDED", updatedComplaint.Status);
        Assert.Equal("R", updatedComplaint.UsageDecision);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-05: Customer Credit Memo
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CR05_IssueCreditMemo_FinancialPosting_Created()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var complaint = await service.CreateComplaintAndReturnOrderAsync(new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            CustomerName = "Acme Foods",
            MaterialCode = "MAT-001",
            ReturnQuantity = 100,
            UnitPrice = 25.00m
        });

        var result = await service.IssueCreditMemoAsync(new CreditMemoRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            BillingType = "RE",
            Amount = 2500m,
            Currency = "INR",
            CostCenter = "CC-001",
            ProfitCenter = "PC-001",
            GLAccount = "400000"
        });

        Assert.True(result.Success);
        Assert.StartsWith("CM-", result.CreditMemoNumber);
        Assert.Equal(2500m, result.Amount);
        Assert.StartsWith("DOC-", result.DocumentNumber);

        // Verify financial posting
        var posting = await db.ComplaintFinancialPostings
            .FirstOrDefaultAsync(p => p.ComplaintReturnId == complaint.ComplaintReturnId);
        Assert.NotNull(posting);
        Assert.Equal("CUSTOMER_CREDIT", posting.PostingType);
        Assert.Equal(0, posting.DebitAmount);
        Assert.Equal(2500m, posting.CreditAmount);
        Assert.Equal("POSTED", posting.Status);

        // Verify complaint updated
        var updatedComplaint = await db.CustomerComplaintReturns.FindAsync(complaint.ComplaintReturnId);
        Assert.Equal("CREDIT_MEMO_ISSUED", updatedComplaint.Status);
        Assert.Equal(2500m, updatedComplaint.CreditMemoAmount);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-06: Supplier Complaint & Claim
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CR06_CreateSupplierComplaint_ClaimLogged()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var complaint = await service.CreateComplaintAndReturnOrderAsync(new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            MaterialCode = "MAT-001",
            SupplierVendorCode = "VEND-001",
            SupplierVendorName = "Global Starch Corp",
            ReturnQuantity = 200,
            UnitPrice = 45.00m
        });

        var result = await service.CreateSupplierComplaintAsync(new SupplierClaimRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            VendorCode = "VEND-001",
            VendorName = "Global Starch Corp",
            MaterialCode = "MAT-001",
            MaterialName = "Corn Starch",
            SupplierBatchNumber = "SUP-BATCH-789",
            PurchaseOrderNumber = "PO-2026-001",
            ClaimQuantity = 200,
            UOM = "KG",
            ClaimAmount = 9000m,
            UnitPrice = 45.00m,
            DefectCode = "RAW-IMPURITY",
            DefectDescription = "Raw material impurity",
            DefectCategory = "SUPPLIER",
            RootCause = "Supplier raw material contamination",
            RootCauseCode = "SUPPLIER-RAW",
            Plant = "PLT-01"
        });

        Assert.True(result.Success);
        Assert.StartsWith("SC-", result.SupplierClaimNumber);
        Assert.Equal(9000m, result.ClaimAmount);
        Assert.StartsWith("QN-SUP-", result.QualityNotificationNumber);

        // Verify supplier claim created
        var supplierClaim = await db.SupplierClaims
            .FirstOrDefaultAsync(s => s.ComplaintReturnId == complaint.ComplaintReturnId);
        Assert.NotNull(supplierClaim);
        Assert.Equal("VEND-001", supplierClaim.VendorCode);
        Assert.Equal("SUPPLIER-RAW", supplierClaim.RootCauseCode);

        // Verify complaint updated
        var updatedComplaint = await db.CustomerComplaintReturns.FindAsync(complaint.ComplaintReturnId);
        Assert.Equal("SUPPLIER_CLAIM_CREATED", updatedComplaint.Status);
        Assert.Equal(9000m, updatedComplaint.SupplierClaimAmount);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-07: Supplier Return Delivery (Mvt 122)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CR07_PostSupplierReturn_MovementType122()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var complaint = await service.CreateComplaintAndReturnOrderAsync(new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            MaterialCode = "MAT-001",
            SupplierVendorCode = "VEND-001",
            ReturnQuantity = 100,
            UnitPrice = 50.00m
        });

        var supplierClaim = await service.CreateSupplierComplaintAsync(new SupplierClaimRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            VendorCode = "VEND-001",
            VendorName = "Global Starch Corp",
            MaterialCode = "MAT-001",
            ClaimQuantity = 100,
            ClaimAmount = 5000m,
            UnitPrice = 50.00m
        });

        var result = await service.PostSupplierReturnDeliveryAsync(new SupplierReturnRequest
        {
            SupplierClaimId = supplierClaim.SupplierClaimId,
            DeliveryNumber = "DEL-SUP-001",
            Quantity = 100,
            BatchNumber = "SUP-BATCH-001",
            VendorCode = "VEND-001",
            PurchaseOrderNumber = "PO-2026-001",
            Plant = "PLT-01",
            StorageLocation = "SL-01"
        });

        Assert.True(result.Success);
        Assert.Equal(122, result.MovementType);
        Assert.Equal(100, result.Quantity);
        Assert.StartsWith("MD-SUP-", result.MaterialDocumentNumber);

        // Verify supplier return delivery created
        var supplierReturn = await db.SupplierReturnDeliveries
            .FirstOrDefaultAsync(s => s.SupplierClaimId == supplierClaim.SupplierClaimId);
        Assert.NotNull(supplierReturn);
        Assert.Equal(122, supplierReturn.MovementType);
        Assert.Equal("BLOCKED", supplierReturn.StockType);

        // Verify stock movement
        var stockMovement = await db.StockMovements
            .FirstOrDefaultAsync(s => s.MovementType == "122");
        Assert.NotNull(stockMovement);
        Assert.Equal(100, stockMovement.Quantity);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-08: Supplier Credit Recovery (Debit Memo)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CR08_IssueDebitMemo_RecoveryCompleted()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var complaint = await service.CreateComplaintAndReturnOrderAsync(new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            MaterialCode = "MAT-001",
            SupplierVendorCode = "VEND-001",
            SupplierVendorName = "Global Starch Corp",
            ReturnQuantity = 200,
            UnitPrice = 45.00m
        });

        var supplierClaim = await service.CreateSupplierComplaintAsync(new SupplierClaimRequest
        {
            ComplaintReturnId = complaint.ComplaintReturnId,
            VendorCode = "VEND-001",
            VendorName = "Global Starch Corp",
            MaterialCode = "MAT-001",
            ClaimQuantity = 200,
            ClaimAmount = 9000m,
            UnitPrice = 45.00m
        });

        var result = await service.IssueDebitMemoAsync(new DebitMemoRequest
        {
            SupplierClaimId = supplierClaim.SupplierClaimId,
            VendorCode = "VEND-001",
            VendorName = "Global Starch Corp",
            Amount = 9000m,
            Currency = "INR",
            PurchaseOrderReference = "PO-2026-001",
            CostCenter = "CC-001",
            ProfitCenter = "PC-001",
            GLAccount = "210000"
        });

        Assert.True(result.Success);
        Assert.StartsWith("DM-", result.DebitMemoNumber);
        Assert.Equal(9000m, result.Amount);
        Assert.StartsWith("DOC-SUP-", result.DocumentNumber);

        // Verify financial posting
        var posting = await db.ComplaintFinancialPostings
            .FirstOrDefaultAsync(p => p.ComplaintReturnId == complaint.ComplaintReturnId &&
                                     p.PostingType == "SUPPLIER_DEBIT");
        Assert.NotNull(posting);
        Assert.Equal(9000m, posting.DebitAmount);
        Assert.Equal(0, posting.CreditAmount);
        Assert.Equal("POSTED", posting.Status);

        // Verify supplier claim updated
        var updatedClaim = await db.SupplierClaims.FindAsync(supplierClaim.SupplierClaimId);
        Assert.Equal("RECOVERY_COMPLETED", updatedClaim.Status);
        Assert.NotNull(updatedClaim.DebitMemoNumber);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Full Workflow Integration Test
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FULL_WORKFLOW_ExecuteAllSteps_CustomerCredit_SupplierRecovery()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var request = new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            CustomerName = "Acme Foods Ltd",
            MaterialCode = "MAT-CORN-STARCH",
            MaterialName = "Corn Starch",
            ReturnQuantity = 500,
            UOM = "KG",
            UnitPrice = 45.00m,
            BatchNumber = "BATCH-2026-001",
            DefectCode = "RAW-IMPURITY",
            DefectDescription = "Raw material impurity detected",
            DefectCategory = "SUPPLIER",
            SupplierVendorCode = "VEND-001",
            SupplierVendorName = "Global Starch Corp",
            SupplierBatchNumber = "SUP-BATCH-789",
            PurchaseOrderReference = "PO-2026-001",
            Plant = "PLT-01",
            CostCenter = "CC-QM-01",
            ProfitCenter = "PC-PROD-01"
        };

        var result = await service.ExecuteFullWorkflowAsync(request);

        Assert.True(result.Success);
        Assert.Equal("CLOSED", result.Status);
        Assert.Equal("COMPLETED", result.CurrentStep);
        Assert.Equal(22500m, result.ReturnAmount);

        // Verify all financial postings
        var postings = await db.ComplaintFinancialPostings
            .Where(p => p.ComplaintReturnId == result.ComplaintReturnId)
            .ToListAsync();
        Assert.Equal(2, postings.Count);

        var creditPost = postings.First(p => p.PostingType == "CUSTOMER_CREDIT");
        Assert.Equal(22500m, creditPost.CreditAmount);

        var debitPost = postings.First(p => p.PostingType == "SUPPLIER_DEBIT");
        Assert.Equal(22500m, debitPost.DebitAmount);

        // Verify workflow steps
        var steps = await db.ComplaintWorkflowSteps
            .Where(s => s.ComplaintReturnId == result.ComplaintReturnId)
            .ToListAsync();
        Assert.All(steps, s => Assert.Equal("COMPLETED", s.Status));

        // Verify all movements posted
        var movements = await db.StockMovements
            .Where(s => s.Reference == result.ComplaintNumber)
            .ToListAsync();
        Assert.True(movements.Count >= 2); // At least return receipt + blocked stock + supplier return
    }

    [Fact]
    public async Task FULL_WORKFLOW_GetProgress_ReturnsAllSteps()
    {
        var db = CreateDb();
        var service = CreateService(db);

        var complaint = await service.CreateComplaintAndReturnOrderAsync(new ComplaintReturnRequest
        {
            TenantId = Guid.NewGuid(),
            CustomerCode = "CUST-001",
            MaterialCode = "MAT-001",
            ReturnQuantity = 100,
            UnitPrice = 20.00m
        });

        var progress = await service.GetWorkflowProgressAsync(complaint.ComplaintReturnId);

        Assert.Equal(8, progress.Count);
        Assert.Contains(progress, p => p.StepCode == "CR-01" && p.Status == "COMPLETED");
        Assert.Contains(progress, p => p.StepCode == "CR-02" && p.Status == "PENDING");
        Assert.Contains(progress, p => p.StepCode == "CR-08" && p.Status == "PENDING");
        Assert.True(progress.First(p => p.StepCode == "CR-01").IsCurrentStep);
    }
}
