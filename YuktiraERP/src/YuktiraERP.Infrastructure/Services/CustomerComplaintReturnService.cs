using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class CustomerComplaintReturnService : ICustomerComplaintReturnService
{
    private readonly YuktiraDbContext _db;

    public CustomerComplaintReturnService(YuktiraDbContext db) => _db = db;

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-01: Create Customer Complaint & Return Order
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<ComplaintReturnResult> CreateComplaintAndReturnOrderAsync(ComplaintReturnRequest request)
    {
        var complaintNumber = $"CR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var returnOrderNumber = $"RE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var qualityNotificationNumber = $"QN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var complaint = new CustomerComplaintReturnEntity
        {
            TenantId = request.TenantId,
            ComplaintNumber = complaintNumber,
            ComplaintType = "Q1",
            ReturnType = "RE",
            CustomerCode = request.CustomerCode,
            CustomerName = request.CustomerName,
            MaterialCode = request.MaterialCode,
            MaterialName = request.MaterialName,
            ReturnQuantity = request.ReturnQuantity,
            UOM = request.UOM,
            UnitPrice = request.UnitPrice,
            ReturnAmount = request.ReturnQuantity * request.UnitPrice,
            BatchNumber = request.BatchNumber,
            DefectCode = request.DefectCode,
            DefectDescription = request.DefectDescription,
            DefectCategory = request.DefectCategory,
            SupplierVendorCode = request.SupplierVendorCode,
            SupplierVendorName = request.SupplierVendorName,
            SupplierBatchNumber = request.SupplierBatchNumber,
            PurchaseOrderReference = request.PurchaseOrderReference,
            ReturnOrderNumber = returnOrderNumber,
            QualityNotificationNumber = qualityNotificationNumber,
            Plant = request.Plant,
            StorageLocation = request.StorageLocation,
            CostCenter = request.CostCenter,
            ProfitCenter = request.ProfitCenter,
            Priority = request.Priority,
            Notes = request.Notes,
            Status = "CREATED",
            CurrentStep = "CR-01",
            ComplaintDate = DateTime.UtcNow
        };

        _db.CustomerComplaintReturns.Add(complaint);

        // Create workflow steps
        var steps = new List<ComplaintWorkflowStepEntity>
        {
            new() { ComplaintReturnId = complaint.Id, StepName = "Customer Complaint Logged", StepCode = "CR-01", StepOrder = 1, Module = "QM", TransactionCode = "QM01", Status = "COMPLETED", CompletedAt = DateTime.UtcNow },
            new() { ComplaintReturnId = complaint.Id, StepName = "Return Delivery & Goods Receipt", StepCode = "CR-02", StepOrder = 2, Module = "MM", TransactionCode = "MIGO", Status = "PENDING" },
            new() { ComplaintReturnId = complaint.Id, StepName = "Root-Cause Analysis", StepCode = "CR-03", StepOrder = 3, Module = "QM", TransactionCode = "QE51N", Status = "PENDING" },
            new() { ComplaintReturnId = complaint.Id, StepName = "Usage Decision", StepCode = "CR-04", StepOrder = 4, Module = "QM", TransactionCode = "QA11", Status = "PENDING" },
            new() { ComplaintReturnId = complaint.Id, StepName = "Customer Credit Memo", StepCode = "CR-05", StepOrder = 5, Module = "SD", TransactionCode = "VF01", Status = "PENDING" },
            new() { ComplaintReturnId = complaint.Id, StepName = "Supplier Complaint", StepCode = "CR-06", StepOrder = 6, Module = "QM", TransactionCode = "QM01", Status = "PENDING" },
            new() { ComplaintReturnId = complaint.Id, StepName = "Supplier Return Delivery", StepCode = "CR-07", StepOrder = 7, Module = "MM", TransactionCode = "MIGO", Status = "PENDING" },
            new() { ComplaintReturnId = complaint.Id, StepName = "Supplier Credit Recovery", StepCode = "CR-08", StepOrder = 8, Module = "FI", TransactionCode = "MIRO", Status = "PENDING" },
        };

        _db.ComplaintWorkflowSteps.AddRange(steps);
        await _db.SaveChangesAsync();

        return new ComplaintReturnResult
        {
            Success = true,
            Message = $"Customer complaint {complaintNumber} created with return order {returnOrderNumber}",
            ComplaintReturnId = complaint.Id,
            ComplaintNumber = complaintNumber,
            ReturnOrderNumber = returnOrderNumber,
            QualityNotificationNumber = qualityNotificationNumber,
            ReturnAmount = complaint.ReturnAmount,
            Status = "CREATED",
            CurrentStep = "CR-01"
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-02: Post Return Delivery & Goods Receipt (Movement Type 651)
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<ReturnDeliveryResult> PostReturnDeliveryAsync(ReturnDeliveryRequest request)
    {
        var complaint = await _db.CustomerComplaintReturns.FindAsync(request.ComplaintReturnId);
        if (complaint == null)
            return new ReturnDeliveryResult { Success = false, Message = "Complaint not found" };

        var materialDocNumber = $"MD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var inspectionLotNumber = $"IL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var returnDelivery = new ReturnDeliveryEntity
        {
            TenantId = complaint.TenantId,
            ComplaintReturnId = complaint.Id,
            ReturnOrderNumber = complaint.ReturnOrderNumber,
            DeliveryNumber = request.DeliveryNumber,
            MaterialDocumentNumber = materialDocNumber,
            MovementType = 651,
            MovementTypeDescription = "Return Delivery to Customer",
            MaterialCode = complaint.MaterialCode,
            MaterialName = complaint.MaterialName,
            Quantity = request.Quantity,
            UOM = complaint.UOM,
            UnitPrice = complaint.UnitPrice,
            BatchNumber = request.BatchNumber,
            Plant = request.Plant,
            StorageLocation = request.StorageLocation,
            StockType = "QI",
            StockTypeDescription = "Quality Inspection",
            CustomerCode = complaint.CustomerCode,
            CustomerName = complaint.CustomerName,
            PostingDate = request.PostingDate,
            DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Reference = complaint.ComplaintNumber,
            HeaderText = $"Return from {complaint.CustomerName} - {complaint.MaterialName}",
            Status = "POSTED",
            PostedBy = "SYSTEM",
            PostedAt = DateTime.UtcNow,
            Notes = request.Notes
        };

        _db.ReturnDeliveries.Add(returnDelivery);

        // Update complaint
        complaint.InspectionLotNumber = inspectionLotNumber;
        complaint.ReturnReceivedDate = DateTime.UtcNow;
        complaint.Status = "RETURN_RECEIVED";
        complaint.CurrentStep = "CR-02";

        // Update workflow step
        var step = await _db.ComplaintWorkflowSteps
            .FirstOrDefaultAsync(s => s.ComplaintReturnId == complaint.Id && s.StepCode == "CR-02");
        if (step != null)
        {
            step.Status = "COMPLETED";
            step.DocumentNumber = materialDocNumber;
            step.CompletedAt = DateTime.UtcNow;
        }

        // Create stock movement
        var stockMovement = new StockMovementEntity
        {
            TenantId = complaint.TenantId,
            DocumentNumber = materialDocNumber,
            MaterialName = complaint.MaterialName,
            MovementType = "651",
            Quantity = request.Quantity,
            StockBefore = 0,
            StockAfter = request.Quantity,
            Reference = complaint.ComplaintNumber,
            Status = "Posted"
        };

        _db.StockMovements.Add(stockMovement);
        await _db.SaveChangesAsync();

        return new ReturnDeliveryResult
        {
            Success = true,
            Message = $"Return delivery posted. Material document: {materialDocNumber}, Inspection lot: {inspectionLotNumber}",
            ReturnDeliveryId = returnDelivery.Id,
            MaterialDocumentNumber = materialDocNumber,
            InspectionLotNumber = inspectionLotNumber,
            MovementType = 651,
            StockType = "QI"
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-03: Record Quality Inspection Root-Cause Analysis
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<QualityInspectionResult> RecordInspectionResultsAsync(QualityInspectionRequest request)
    {
        var complaint = await _db.CustomerComplaintReturns.FindAsync(request.ComplaintReturnId);
        if (complaint == null)
            return new QualityInspectionResult { Success = false, Message = "Complaint not found" };

        var inspection = new QualityInspectionReturnEntity
        {
            TenantId = complaint.TenantId,
            ComplaintReturnId = complaint.Id,
            InspectionLotNumber = request.InspectionLotNumber,
            MaterialCode = complaint.MaterialCode,
            MaterialName = complaint.MaterialName,
            BatchNumber = complaint.BatchNumber,
            SupplierBatchNumber = complaint.SupplierBatchNumber,
            Quantity = complaint.ReturnQuantity,
            UOM = complaint.UOM,
            Plant = complaint.Plant,
            InspectionType = "RETURN",
            InspectionLotOrigin = "08",
            Characteristic = request.Characteristic,
            Specification = request.Specification,
            ResultValue = request.ResultValue,
            ResultValuation = request.ResultValuation,
            DefectCodeGroup = request.DefectCodeGroup,
            DefectCode = request.DefectCode,
            DefectDescription = request.DefectDescription,
            DefectCategory = request.DefectCategory,
            RootCause = request.RootCause,
            RootCauseCode = request.RootCauseCode,
            Status = "RECORDED",
            RecordedBy = request.RecordedBy,
            RecordedAt = DateTime.UtcNow
        };

        _db.QualityInspectionReturns.Add(inspection);

        // Determine if defect is supplier-related
        bool isSupplierDefect = request.RootCauseCode?.StartsWith("SUPPLIER") == true ||
                                request.DefectCategory == "SUPPLIER" ||
                                request.RootCause?.Contains("supplier", StringComparison.OrdinalIgnoreCase) == true ||
                                request.RootCause?.Contains("vendor", StringComparison.OrdinalIgnoreCase) == true;

        // Update complaint
        complaint.RootCause = request.RootCause;
        complaint.RootCauseCode = request.RootCauseCode;
        complaint.InspectionCompletedDate = DateTime.UtcNow;
        complaint.Status = "INSPECTION_COMPLETED";
        complaint.CurrentStep = "CR-03";

        // Update workflow step
        var step = await _db.ComplaintWorkflowSteps
            .FirstOrDefaultAsync(s => s.ComplaintReturnId == complaint.Id && s.StepCode == "CR-03");
        if (step != null)
        {
            step.Status = "COMPLETED";
            step.DocumentNumber = request.InspectionLotNumber;
            step.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return new QualityInspectionResult
        {
            Success = true,
            Message = isSupplierDefect
                ? $"Root cause traced to supplier batch {complaint.SupplierBatchNumber}. Supplier claim required."
                : $"Root cause analysis completed: {request.RootCause}",
            InspectionReturnId = inspection.Id,
            RootCause = request.RootCause,
            DefectCode = request.DefectCode,
            IsSupplierDefect = isSupplierDefect
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-04: Post Usage Decision (Reject → Blocked Stock)
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<UsageDecisionResult> PostUsageDecisionAsync(UsageDecisionRequest request)
    {
        var complaint = await _db.CustomerComplaintReturns.FindAsync(request.ComplaintReturnId);
        if (complaint == null)
            return new UsageDecisionResult { Success = false, Message = "Complaint not found" };

        var inspection = await _db.QualityInspectionReturns
            .FirstOrDefaultAsync(i => i.ComplaintReturnId == complaint.Id);

        if (inspection != null)
        {
            inspection.UsageDecision = request.UsageDecision;
            inspection.UsageDecisionCode = request.UsageDecisionCode;
            inspection.StockProposal = request.StockProposal;
            inspection.TargetStockType = request.TargetStockType;
            inspection.DecidedBy = request.DecidedBy;
            inspection.DecisionDate = DateTime.UtcNow;
            inspection.Status = "DECIDED";
        }

        // Create stock movement for blocked stock (Mvt 349)
        var materialDocNumber = $"MD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var stockMovement = new StockMovementEntity
        {
            TenantId = complaint.TenantId,
            DocumentNumber = materialDocNumber,
            MaterialName = complaint.MaterialName,
            MovementType = "349",
            Quantity = complaint.ReturnQuantity,
            StockBefore = complaint.ReturnQuantity,
            StockAfter = 0,
            Reference = complaint.ComplaintNumber,
            Status = "Posted"
        };

        _db.StockMovements.Add(stockMovement);

        // Update complaint
        complaint.UsageDecision = request.UsageDecision;
        complaint.StockProposal = request.StockProposal;
        complaint.Status = "USAGE_DECIDED";
        complaint.CurrentStep = "CR-04";

        // Update workflow step
        var step = await _db.ComplaintWorkflowSteps
            .FirstOrDefaultAsync(s => s.ComplaintReturnId == complaint.Id && s.StepCode == "CR-04");
        if (step != null)
        {
            step.Status = "COMPLETED";
            step.DocumentNumber = materialDocNumber;
            step.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        bool requiresSupplierClaim = inspection?.IsSupplierDefect() == true ||
                                     complaint.RootCauseCode?.StartsWith("SUPPLIER") == true;

        return new UsageDecisionResult
        {
            Success = true,
            Message = $"Usage decision '{request.UsageDecision}' posted. Stock moved to {request.TargetStockType}",
            InspectionReturnId = inspection?.Id ?? Guid.Empty,
            UsageDecision = request.UsageDecision,
            StockType = request.TargetStockType,
            RequiresSupplierClaim = requiresSupplierClaim
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-05: Issue Customer Credit Memo
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<CreditMemoResult> IssueCreditMemoAsync(CreditMemoRequest request)
    {
        var complaint = await _db.CustomerComplaintReturns.FindAsync(request.ComplaintReturnId);
        if (complaint == null)
            return new CreditMemoResult { Success = false, Message = "Complaint not found" };

        var creditMemoNumber = $"CM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var docNumber = $"DOC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        // Create financial posting
        var posting = new ComplaintFinancialPostingEntity
        {
            TenantId = complaint.TenantId,
            ComplaintReturnId = complaint.Id,
            DocumentNumber = docNumber,
            DocumentType = "Credit Memo",
            PostingType = "CUSTOMER_CREDIT",
            AccountCode = complaint.CustomerCode,
            AccountName = complaint.CustomerName,
            PartyCode = complaint.CustomerCode,
            PartyName = complaint.CustomerName,
            DebitAmount = 0,
            CreditAmount = request.Amount,
            Amount = request.Amount,
            Currency = request.Currency,
            Reference = complaint.ComplaintNumber,
            Description = $"Customer credit memo for complaint {complaint.ComplaintNumber}",
            CostCenter = request.CostCenter,
            ProfitCenter = request.ProfitCenter,
            GLAccount = request.GLAccount,
            PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Period = DateTime.UtcNow.ToString("yyyy-MM"),
            FiscalYear = DateTime.UtcNow.Year.ToString(),
            Status = "POSTED",
            PostedBy = "SYSTEM",
            PostedAt = DateTime.UtcNow
        };

        _db.ComplaintFinancialPostings.Add(posting);

        // Update complaint
        complaint.CreditMemoNumber = creditMemoNumber;
        complaint.CreditMemoAmount = request.Amount;
        complaint.CreditMemoIssuedDate = DateTime.UtcNow;
        complaint.Status = "CREDIT_MEMO_ISSUED";
        complaint.CurrentStep = "CR-05";

        // Update workflow step
        var step = await _db.ComplaintWorkflowSteps
            .FirstOrDefaultAsync(s => s.ComplaintReturnId == complaint.Id && s.StepCode == "CR-05");
        if (step != null)
        {
            step.Status = "COMPLETED";
            step.DocumentNumber = creditMemoNumber;
            step.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return new CreditMemoResult
        {
            Success = true,
            Message = $"Credit memo {creditMemoNumber} issued for {request.Amount} {request.Currency}",
            CreditMemoNumber = creditMemoNumber,
            Amount = request.Amount,
            DocumentNumber = docNumber
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-06: Create Supplier Complaint
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<SupplierClaimResult> CreateSupplierComplaintAsync(SupplierClaimRequest request)
    {
        var complaint = await _db.CustomerComplaintReturns.FindAsync(request.ComplaintReturnId);
        if (complaint == null)
            return new SupplierClaimResult { Success = false, Message = "Complaint not found" };

        var supplierClaimNumber = $"SC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var qualityNotificationNumber = $"QN-SUP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var supplierClaim = new SupplierClaimEntity
        {
            TenantId = complaint.TenantId,
            ComplaintReturnId = complaint.Id,
            SupplierClaimNumber = supplierClaimNumber,
            SupplierComplaintType = "Q2",
            VendorCode = request.VendorCode,
            VendorName = request.VendorName,
            MaterialCode = request.MaterialCode,
            MaterialName = request.MaterialName,
            SupplierBatchNumber = request.SupplierBatchNumber,
            PurchaseOrderNumber = request.PurchaseOrderNumber,
            ClaimQuantity = request.ClaimQuantity,
            UOM = request.UOM,
            ClaimAmount = request.ClaimAmount,
            UnitPrice = request.UnitPrice,
            DefectCode = request.DefectCode,
            DefectDescription = request.DefectDescription,
            DefectCategory = request.DefectCategory,
            RootCause = request.RootCause,
            RootCauseCode = request.RootCauseCode,
            CustomerComplaintReference = complaint.ComplaintNumber,
            CustomerComplaintNumber = complaint.ComplaintNumber,
            CustomerReturnNumber = complaint.ReturnOrderNumber,
            QualityNotificationNumber = qualityNotificationNumber,
            Plant = request.Plant,
            CostCenter = request.CostCenter,
            ProfitCenter = request.ProfitCenter,
            Priority = complaint.Priority,
            Status = "CREATED",
            CurrentStep = "CR-06",
            ClaimCreatedDate = DateTime.UtcNow,
            Notes = request.Notes
        };

        _db.SupplierClaims.Add(supplierClaim);

        // Update complaint
        complaint.SupplierClaimNumber = supplierClaimNumber;
        complaint.SupplierClaimAmount = request.ClaimAmount;
        complaint.SupplierClaimCreatedDate = DateTime.UtcNow;
        complaint.Status = "SUPPLIER_CLAIM_CREATED";
        complaint.CurrentStep = "CR-06";

        // Update workflow step
        var step = await _db.ComplaintWorkflowSteps
            .FirstOrDefaultAsync(s => s.ComplaintReturnId == complaint.Id && s.StepCode == "CR-06");
        if (step != null)
        {
            step.Status = "COMPLETED";
            step.DocumentNumber = supplierClaimNumber;
            step.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return new SupplierClaimResult
        {
            Success = true,
            Message = $"Supplier complaint {supplierClaimNumber} created. Quality notification {qualityNotificationNumber} logged.",
            SupplierClaimId = supplierClaim.Id,
            SupplierClaimNumber = supplierClaimNumber,
            ClaimAmount = request.ClaimAmount,
            QualityNotificationNumber = qualityNotificationNumber
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-07: Post Supplier Return Delivery (Movement Type 122)
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<SupplierReturnResult> PostSupplierReturnDeliveryAsync(SupplierReturnRequest request)
    {
        var supplierClaim = await _db.SupplierClaims.FindAsync(request.SupplierClaimId);
        if (supplierClaim == null)
            return new SupplierReturnResult { Success = false, Message = "Supplier claim not found" };

        var materialDocNumber = $"MD-SUP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var supplierReturn = new SupplierReturnDeliveryEntity
        {
            TenantId = supplierClaim.TenantId,
            SupplierClaimId = supplierClaim.Id,
            SupplierReturnNumber = $"SR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            DeliveryNumber = request.DeliveryNumber,
            MaterialDocumentNumber = materialDocNumber,
            MovementType = 122,
            MovementTypeDescription = "Return to Vendor",
            MaterialCode = supplierClaim.MaterialCode,
            MaterialName = supplierClaim.MaterialName,
            Quantity = request.Quantity,
            UOM = supplierClaim.UOM,
            UnitPrice = supplierClaim.UnitPrice,
            TotalValue = request.Quantity * supplierClaim.UnitPrice,
            BatchNumber = request.BatchNumber,
            VendorCode = request.VendorCode,
            VendorName = supplierClaim.VendorName,
            PurchaseOrderNumber = request.PurchaseOrderNumber,
            Plant = request.Plant,
            StorageLocation = request.StorageLocation,
            StockType = "BLOCKED",
            PostingDate = request.PostingDate,
            DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Reference = supplierClaim.SupplierClaimNumber,
            HeaderText = $"Supplier return to {supplierClaim.VendorName} - {supplierClaim.MaterialName}",
            Status = "POSTED",
            PostedBy = "SYSTEM",
            PostedAt = DateTime.UtcNow,
            Notes = request.Notes
        };

        _db.SupplierReturnDeliveries.Add(supplierReturn);

        // Update supplier claim
        supplierClaim.SupplierReturnDeliveryNumber = supplierReturn.SupplierReturnNumber;
        supplierClaim.SupplierReturnMaterialDocument = materialDocNumber;
        supplierClaim.SupplierReturnMovementType = 122;
        supplierClaim.SupplierReturnDate = DateTime.UtcNow;
        supplierClaim.Status = "SUPPLIER_RETURN_POSTED";
        supplierClaim.CurrentStep = "CR-07";

        // Update complaint
        var complaint = await _db.CustomerComplaintReturns.FindAsync(supplierClaim.ComplaintReturnId);
        if (complaint != null)
        {
            complaint.SupplierReturnDeliveryNumber = supplierReturn.SupplierReturnNumber;
            complaint.SupplierReturnDate = DateTime.UtcNow;
            complaint.Status = "SUPPLIER_RETURN_POSTED";
            complaint.CurrentStep = "CR-07";
        }

        // Create stock movement
        var stockMovement = new StockMovementEntity
        {
            TenantId = supplierClaim.TenantId,
            DocumentNumber = materialDocNumber,
            MaterialName = supplierClaim.MaterialName,
            MovementType = "122",
            Quantity = request.Quantity,
            StockBefore = request.Quantity,
            StockAfter = 0,
            Reference = supplierClaim.SupplierClaimNumber,
            Status = "Posted"
        };

        _db.StockMovements.Add(stockMovement);

        // Update workflow step
        var step = await _db.ComplaintWorkflowSteps
            .FirstOrDefaultAsync(s => s.ComplaintReturnId == supplierClaim.ComplaintReturnId && s.StepCode == "CR-07");
        if (step != null)
        {
            step.Status = "COMPLETED";
            step.DocumentNumber = materialDocNumber;
            step.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return new SupplierReturnResult
        {
            Success = true,
            Message = $"Supplier return posted. Material document: {materialDocNumber}, Movement type: 122",
            SupplierReturnDeliveryId = supplierReturn.Id,
            MaterialDocumentNumber = materialDocNumber,
            MovementType = 122,
            Quantity = request.Quantity
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CR-08: Issue Supplier Credit Recovery (Debit Memo)
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<DebitMemoResult> IssueDebitMemoAsync(DebitMemoRequest request)
    {
        var supplierClaim = await _db.SupplierClaims.FindAsync(request.SupplierClaimId);
        if (supplierClaim == null)
            return new DebitMemoResult { Success = false, Message = "Supplier claim not found" };

        var debitMemoNumber = $"DM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var docNumber = $"DOC-SUP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        // Create financial posting
        var posting = new ComplaintFinancialPostingEntity
        {
            TenantId = supplierClaim.TenantId,
            ComplaintReturnId = supplierClaim.ComplaintReturnId,
            DocumentNumber = docNumber,
            DocumentType = "Debit Memo",
            PostingType = "SUPPLIER_DEBIT",
            AccountCode = request.VendorCode,
            AccountName = request.VendorName,
            PartyCode = request.VendorCode,
            PartyName = request.VendorName,
            DebitAmount = request.Amount,
            CreditAmount = 0,
            Amount = request.Amount,
            Currency = request.Currency,
            Reference = supplierClaim.SupplierClaimNumber,
            Description = $"Supplier debit memo for claim {supplierClaim.SupplierClaimNumber}",
            CostCenter = request.CostCenter,
            ProfitCenter = request.ProfitCenter,
            GLAccount = request.GLAccount,
            PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Period = DateTime.UtcNow.ToString("yyyy-MM"),
            FiscalYear = DateTime.UtcNow.Year.ToString(),
            Status = "POSTED",
            PostedBy = "SYSTEM",
            PostedAt = DateTime.UtcNow
        };

        _db.ComplaintFinancialPostings.Add(posting);

        // Update supplier claim
        supplierClaim.DebitMemoNumber = debitMemoNumber;
        supplierClaim.RecoveryCompletedDate = DateTime.UtcNow;
        supplierClaim.Status = "RECOVERY_COMPLETED";
        supplierClaim.CurrentStep = "CR-08";

        // Update complaint
        var complaint = await _db.CustomerComplaintReturns.FindAsync(supplierClaim.ComplaintReturnId);
        if (complaint != null)
        {
            complaint.SupplierDebitMemoNumber = debitMemoNumber;
            complaint.RecoveryAmount = request.Amount;
            complaint.RecoveryCompletedDate = DateTime.UtcNow;
            complaint.Status = "RECOVERY_COMPLETED";
            complaint.CurrentStep = "CR-08";
        }

        // Update workflow step
        var step = await _db.ComplaintWorkflowSteps
            .FirstOrDefaultAsync(s => s.ComplaintReturnId == supplierClaim.ComplaintReturnId && s.StepCode == "CR-08");
        if (step != null)
        {
            step.Status = "COMPLETED";
            step.DocumentNumber = debitMemoNumber;
            step.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return new DebitMemoResult
        {
            Success = true,
            Message = $"Debit memo {debitMemoNumber} issued. Recovery amount: {request.Amount} {request.Currency}",
            DebitMemoNumber = debitMemoNumber,
            Amount = request.Amount,
            DocumentNumber = docNumber
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Execute Full Workflow (Orchestration)
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<ComplaintReturnResult> ExecuteFullWorkflowAsync(ComplaintReturnRequest request)
    {
        // CR-01: Create complaint
        var result = await CreateComplaintAndReturnOrderAsync(request);
        if (!result.Success) return result;

        // CR-02: Post return delivery
        var returnResult = await PostReturnDeliveryAsync(new ReturnDeliveryRequest
        {
            ComplaintReturnId = result.ComplaintReturnId,
            Quantity = request.ReturnQuantity,
            BatchNumber = request.BatchNumber,
            Plant = request.Plant,
            StorageLocation = request.StorageLocation
        });

        // CR-03: Record inspection
        var inspectionResult = await RecordInspectionResultsAsync(new QualityInspectionRequest
        {
            ComplaintReturnId = result.ComplaintReturnId,
            InspectionLotNumber = returnResult.InspectionLotNumber,
            DefectCode = request.DefectCode,
            DefectDescription = request.DefectDescription,
            DefectCategory = request.DefectCategory,
            RootCause = request.DefectDescription,
            RootCauseCode = $"SUPPLIER-{request.DefectCode}",
            RecordedBy = "SYSTEM"
        });

        // CR-04: Usage decision
        var udResult = await PostUsageDecisionAsync(new UsageDecisionRequest
        {
            ComplaintReturnId = result.ComplaintReturnId,
            UsageDecision = "R",
            UsageDecisionCode = "R",
            StockProposal = "349",
            TargetStockType = "BLOCKED",
            DecidedBy = "SYSTEM"
        });

        // CR-05: Customer credit memo
        var creditResult = await IssueCreditMemoAsync(new CreditMemoRequest
        {
            ComplaintReturnId = result.ComplaintReturnId,
            Amount = result.ReturnAmount,
            CostCenter = request.CostCenter,
            ProfitCenter = request.ProfitCenter
        });

        // CR-06: Supplier complaint
        var supplierClaimResult = await CreateSupplierComplaintAsync(new SupplierClaimRequest
        {
            ComplaintReturnId = result.ComplaintReturnId,
            VendorCode = request.SupplierVendorCode,
            VendorName = request.SupplierVendorName,
            MaterialCode = request.MaterialCode,
            MaterialName = request.MaterialName,
            SupplierBatchNumber = request.SupplierBatchNumber,
            ClaimQuantity = request.ReturnQuantity,
            UOM = request.UOM,
            ClaimAmount = result.ReturnAmount,
            UnitPrice = request.UnitPrice,
            DefectCode = request.DefectCode,
            DefectDescription = request.DefectDescription,
            DefectCategory = request.DefectCategory,
            RootCause = request.DefectDescription,
            RootCauseCode = $"SUPPLIER-{request.DefectCode}",
            Plant = request.Plant,
            CostCenter = request.CostCenter,
            ProfitCenter = request.ProfitCenter
        });

        // CR-07: Supplier return
        var supplierReturnResult = await PostSupplierReturnDeliveryAsync(new SupplierReturnRequest
        {
            SupplierClaimId = supplierClaimResult.SupplierClaimId,
            Quantity = request.ReturnQuantity,
            BatchNumber = request.BatchNumber,
            VendorCode = request.SupplierVendorCode,
            PurchaseOrderNumber = request.PurchaseOrderReference,
            Plant = request.Plant,
            StorageLocation = request.StorageLocation
        });

        // CR-08: Debit memo
        var debitResult = await IssueDebitMemoAsync(new DebitMemoRequest
        {
            SupplierClaimId = supplierClaimResult.SupplierClaimId,
            VendorCode = request.SupplierVendorCode,
            VendorName = request.SupplierVendorName,
            Amount = result.ReturnAmount,
            CostCenter = request.CostCenter,
            ProfitCenter = request.ProfitCenter
        });

        // Mark complaint as closed
        var complaint = await _db.CustomerComplaintReturns.FindAsync(result.ComplaintReturnId);
        if (complaint != null)
        {
            complaint.Status = "CLOSED";
            complaint.CurrentStep = "COMPLETED";
            complaint.ClosedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return new ComplaintReturnResult
        {
            Success = true,
            Message = $"Full workflow completed. Customer credit: {creditResult.Amount}, Supplier recovery: {debitResult.Amount}",
            ComplaintReturnId = result.ComplaintReturnId,
            ComplaintNumber = result.ComplaintNumber,
            ReturnOrderNumber = result.ReturnOrderNumber,
            QualityNotificationNumber = result.QualityNotificationNumber,
            ReturnAmount = result.ReturnAmount,
            Status = "CLOSED",
            CurrentStep = "COMPLETED"
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Get Workflow Progress
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<List<ComplaintWorkflowStep>> GetWorkflowProgressAsync(Guid complaintReturnId)
    {
        var steps = await _db.ComplaintWorkflowSteps
            .Where(s => s.ComplaintReturnId == complaintReturnId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        var complaint = await _db.CustomerComplaintReturns.FindAsync(complaintReturnId);
        var currentStep = complaint?.CurrentStep ?? "";

        return steps.Select(s => new ComplaintWorkflowStep
        {
            StepName = s.StepName,
            StepCode = s.StepCode,
            StepOrder = s.StepOrder,
            Module = s.Module,
            TransactionCode = s.TransactionCode,
            DocumentNumber = s.DocumentNumber,
            Status = s.Status,
            CompletedAt = s.CompletedAt,
            IsCurrentStep = s.StepCode == currentStep
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Get Complaint Details
    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<ComplaintReturnResult> GetComplaintDetailsAsync(Guid complaintReturnId)
    {
        var complaint = await _db.CustomerComplaintReturns.FindAsync(complaintReturnId);
        if (complaint == null)
            return new ComplaintReturnResult { Success = false, Message = "Complaint not found" };

        return new ComplaintReturnResult
        {
            Success = true,
            ComplaintReturnId = complaint.Id,
            ComplaintNumber = complaint.ComplaintNumber,
            ReturnOrderNumber = complaint.ReturnOrderNumber,
            QualityNotificationNumber = complaint.QualityNotificationNumber,
            ReturnAmount = complaint.ReturnAmount,
            Status = complaint.Status,
            CurrentStep = complaint.CurrentStep
        };
    }
}

// Extension method to check supplier defect
public static class QualityInspectionReturnExtensions
{
    public static bool IsSupplierDefect(this QualityInspectionReturnEntity inspection)
    {
        return inspection.RootCauseCode?.StartsWith("SUPPLIER") == true ||
               inspection.DefectCategory == "SUPPLIER" ||
               inspection.RootCause?.Contains("supplier", StringComparison.OrdinalIgnoreCase) == true ||
               inspection.RootCause?.Contains("vendor", StringComparison.OrdinalIgnoreCase) == true;
    }
}
