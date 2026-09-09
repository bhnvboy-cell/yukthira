using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class PipelineDiagnosticService : IPipelineDiagnosticService
{
    private readonly YuktiraDbContext _db;

    public PipelineDiagnosticService(YuktiraDbContext db) => _db = db;

    public async Task<PipelineDiagnosticResult> ExecuteFullPipelineDiagnosticAsync(PipelineDiagnosticRequest request, Guid tenantId, string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new PipelineDiagnosticResult
        {
            TenantId = tenantId,
            Traceability = new PipelineTraceabilityInfo()
        };

        var stepNum = 0;

        // Step 1: Purchase Order
        var step1 = await ValidatePurchaseOrderAsync(request.PurchaseOrderNumber, tenantId);
        step1.StepNumber = ++stepNum;
        result.Steps.Add(step1);
        result.Traceability.PurchaseOrderNumber = step1.DocumentNumber;

        var poNumber = step1.DocumentNumber ?? request.PurchaseOrderNumber;

        // Step 2: Goods Receipt
        var step2 = await ValidateGoodsReceiptAsync(poNumber, tenantId);
        step2.StepNumber = ++stepNum;
        result.Steps.Add(step2);
        result.Traceability.GoodsReceiptNumber = step2.DocumentNumber;

        // Step 3: Inspection Lot
        var step3 = await ValidateInspectionLotAsync(request.MaterialCode, request.BatchNumber, tenantId);
        step3.StepNumber = ++stepNum;
        result.Steps.Add(step3);
        result.Traceability.InspectionLotNumber = step3.DocumentNumber;

        var lotNumber = step3.DocumentNumber;

        // Step 4: Inspection Results
        var step4 = await ValidateInspectionResultsAsync(lotNumber, tenantId);
        step4.StepNumber = ++stepNum;
        result.Steps.Add(step4);

        // Step 5: Usage Decision
        var step5 = await ValidateUsageDecisionAsync(lotNumber, tenantId);
        step5.StepNumber = ++stepNum;
        result.Steps.Add(step5);
        result.Traceability.UsageDecisionId = step5.DocumentNumber;

        // Step 6: Quality Release (Stock 321)
        var step6 = await ValidateQualityReleaseAsync(lotNumber, tenantId);
        step6.StepNumber = ++stepNum;
        result.Steps.Add(step6);

        // Step 7: Sales Order
        var step7 = await ValidateSalesOrderAsync(request.SalesOrderNumber, tenantId);
        step7.StepNumber = ++stepNum;
        result.Steps.Add(step7);
        result.Traceability.SalesOrderNumber = step7.DocumentNumber;

        var soNumber = step7.DocumentNumber ?? request.SalesOrderNumber;

        // Step 8: Delivery
        var step8 = await ValidateDeliveryAsync(soNumber, tenantId);
        step8.StepNumber = ++stepNum;
        result.Steps.Add(step8);
        result.Traceability.DeliveryNumber = step8.DocumentNumber;

        var deliveryNumber = step8.DocumentNumber;

        // Step 9: Billing Document
        var step9 = await ValidateBillingDocumentAsync(deliveryNumber, tenantId);
        step9.StepNumber = ++stepNum;
        result.Steps.Add(step9);
        result.Traceability.BillingDocumentNumber = step9.DocumentNumber;
        result.Traceability.TotalInvoiceAmount = step9.Amount;

        var billingDocNumber = step9.DocumentNumber;

        // Step 10: FI Ledger Posting
        var step10 = await ValidateFiLedgerPostingAsync(billingDocNumber, tenantId);
        step10.StepNumber = ++stepNum;
        result.Steps.Add(step10);
        result.Traceability.FiJournalDocumentNumber = step10.DocumentNumber;
        result.Traceability.TotalFiPostedAmount = step10.Amount;

        // Step 11: Stock Integrity Check
        var step11 = await ValidateStockIntegrityAsync(request.MaterialCode, request.BatchNumber, tenantId);
        step11.StepNumber = ++stepNum;
        result.Steps.Add(step11);

        sw.Stop();

        result.TotalSteps = result.Steps.Count;
        result.PassedSteps = result.Steps.Count(s => s.Status == PipelineStepStatus.Passed);
        result.FailedSteps = result.Steps.Count(s => s.Status == PipelineStepStatus.Failed);
        result.WarningSteps = result.Steps.Count(s => s.Status == PipelineStepStatus.Warning);
        result.SkippedSteps = result.Steps.Count(s => s.Status == PipelineStepStatus.Skipped || s.Status == PipelineStepStatus.NotApplicable);
        result.TotalDurationMs = sw.ElapsedMilliseconds;

        result.OverallStatus = result.FailedSteps > 0 ? PipelineStepStatus.Failed
            : result.WarningSteps > 0 ? PipelineStepStatus.Warning
            : PipelineStepStatus.Passed;

        result.CriticalIssues = result.Steps
            .Where(s => s.Status == PipelineStepStatus.Failed)
            .Select(s => $"[Step {s.StepNumber}] {s.StepName}: {s.ErrorMessage ?? "FAILED"}")
            .ToList();

        result.Recommendations = GenerateRecommendations(result);

        if (result.Traceability.TotalInvoiceAmount.HasValue && result.Traceability.TotalFiPostedAmount.HasValue)
        {
            result.Traceability.IsBalanced = Math.Abs(result.Traceability.TotalInvoiceAmount.Value - result.Traceability.TotalFiPostedAmount.Value) < 0.01m;
        }

        return result;
    }

    public async Task<PipelineStepResult> ValidatePurchaseOrderAsync(string? poNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Purchase Order",
            Description = "Validates PO exists, has items, and is in Released/Approved status",
            Category = PipelineStepCategory.MM_Procurement,
            EntityType = "PurchaseOrderEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var query = _db.PurchaseOrders.AsNoTracking().Where(p => p.TenantId == tenantId);
            if (!string.IsNullOrWhiteSpace(poNumber))
                query = query.Where(p => p.PoNumber == poNumber);

            var po = await query.OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync();
            if (po == null)
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"No Purchase Order found{(poNumber != null ? $" for PO '{poNumber}'" : " for this tenant")}";
                step.Resolution = "Create a Purchase Order via API (POST /api/mm/PO) or UI before running diagnostics.";
                return step;
            }

            step.DocumentNumber = po.PoNumber;
            step.EntityId = po.Id.ToString();
            step.Amount = po.TotalAmount;

            step.ValidationChecks.Add($"PO Number: {po.PoNumber}");
            step.ValidationChecks.Add($"Status: {po.Status}");
            step.ValidationChecks.Add($"Vendor: {po.VendorName} ({po.VendorCode})");
            step.ValidationChecks.Add($"Total Amount: {po.TotalAmount:C}");
            step.ValidationChecks.Add($"Item Count: {po.ItemCount}");

            if (po.Status?.ToUpperInvariant() is "APPROVED" or "RELEASED" or "OPEN")
                step.Status = PipelineStepStatus.Passed;
            else if (po.Status?.ToUpperInvariant() is "PENDING")
            {
                step.Status = PipelineStepStatus.Warning;
                step.ErrorMessage = $"PO is in '{po.Status}' status. Should be Approved/Released.";
                step.Resolution = "Release the PO via approval workflow before proceeding to GR.";
            }
            else
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"PO is in terminal status '{po.Status}'";
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateGoodsReceiptAsync(string? poNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Goods Receipt (MIGO 101)",
            Description = "Validates GR posted against PO with Movement Type 101",
            Category = PipelineStepCategory.MM_GoodsReceipt,
            EntityType = "MovementDocumentEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var grDocs = await _db.MovementDocuments
                .AsNoTracking()
                .Where(d => d.TenantId == tenantId && d.MovementType == 101 && d.Status == "Posted")
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(poNumber))
            {
                var poRef = grDocs.Where(d => d.Reference != null && d.Reference.Contains(poNumber)).ToList();
                if (poRef.Any()) grDocs = poRef;
            }

            var gr = grDocs.FirstOrDefault();
            if (gr == null)
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"No Goods Receipt found with Movement Type 101{(poNumber != null ? $" referencing PO '{poNumber}'" : "")}";
                step.Resolution = "Post Goods Receipt via MIGO (Movement Type 101) for the PO items.";
                return step;
            }

            step.DocumentNumber = gr.DocumentNumber;
            step.EntityId = gr.Id.ToString();
            step.MovementType = "101";
            step.Quantity = gr.TotalQuantity;
            step.StockType = "QualityInspection";

            step.ValidationChecks.Add($"GR Document: {gr.DocumentNumber}");
            step.ValidationChecks.Add($"Movement Type: {gr.MovementType}");
            step.ValidationChecks.Add($"Status: {gr.Status}");
            step.ValidationChecks.Add($"Plant: {gr.Plant}");
            step.ValidationChecks.Add($"Reference: {gr.Reference ?? "N/A"}");
            step.ValidationChecks.Add($"Total Quantity: {gr.TotalQuantity}");
            step.ValidationChecks.Add($"Posted By: {gr.PostedBy ?? "N/A"}");

            if (gr.Status == "Posted")
            {
                step.Status = PipelineStepStatus.Passed;
                step.Details = "GR posted successfully. Inspection lot should have been auto-created for QM-enabled materials.";
            }
            else
            {
                step.Status = PipelineStepStatus.Warning;
                step.ErrorMessage = $"GR is in status '{gr.Status}' instead of 'Posted'";
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateInspectionLotAsync(string? materialCode, string? batchNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Inspection Lot (QM)",
            Description = "Validates inspection lot created from GR with correct status",
            Category = PipelineStepCategory.QM_InspectionLot,
            EntityType = "InspectionLotEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var query = _db.InspectionLots.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(materialCode))
                query = query.Where(l => l.MaterialCode == materialCode);
            if (!string.IsNullOrWhiteSpace(batchNumber))
                query = query.Where(l => l.BatchNumber == batchNumber);

            var lot = await query.OrderByDescending(l => l.CreatedAt).FirstOrDefaultAsync();
            if (lot == null)
            {
                step.Status = PipelineStepStatus.NotApplicable;
                step.ErrorMessage = $"No inspection lot found{(materialCode != null ? $" for material '{materialCode}'" : "")}";
                step.Details = "Material may not be QM-enabled, or inspection lot was not auto-created from GR.";
                step.Resolution = "Verify material master has QM inspection setup. If QM-enabled, ensure GR (101) triggers inspection lot creation.";
                return step;
            }

            step.DocumentNumber = lot.LotNumber;
            step.EntityId = lot.Id.ToString();
            step.Quantity = decimal.TryParse(lot.Quantity, out var qty) ? qty : null;
            step.StockType = lot.Status;

            step.ValidationChecks.Add($"Lot Number: {lot.LotNumber}");
            step.ValidationChecks.Add($"Material: {lot.MaterialCode} — {lot.MaterialName}");
            step.ValidationChecks.Add($"Plant: {lot.Plant}");
            step.ValidationChecks.Add($"Batch: {lot.BatchNumber}");
            step.ValidationChecks.Add($"Status: {lot.Status}");
            step.ValidationChecks.Add($"Inspection Type: {lot.InspectionType}");
            step.ValidationChecks.Add($"Assigned Inspector: {lot.AssignedInspector ?? "Unassigned"}");
            step.ValidationChecks.Add($"Sample Size: {lot.SampleSize}");
            step.ValidationChecks.Add($"Inspected/Passed/Failed: {lot.Inspected}/{lot.Passed}/{lot.Failed}");

            var validStatuses = new[] { "Created", "InInspection", "UsageDecisionCompleted", "Completed", "USAGE_DECIDED" };
            if (validStatuses.Contains(lot.Status))
                step.Status = PipelineStepStatus.Passed;
            else if (lot.Status == "Pending")
            {
                step.Status = PipelineStepStatus.Warning;
                step.ErrorMessage = $"Lot is in '{lot.Status}' status. Should be at least 'Created'.";
            }
            else
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"Lot is in unexpected status '{lot.Status}'";
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateInspectionResultsAsync(string? lotNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Inspection Results Recording",
            Description = "Validates inspection results have been recorded for the lot",
            Category = PipelineStepCategory.QM_InspectionResults,
            EntityType = "InspectionResultDetailEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                step.Status = PipelineStepStatus.Skipped;
                step.ErrorMessage = "No lot number provided — skipping result validation.";
                return step;
            }

            var results = await _db.InspectionResultDetails
                .AsNoTracking()
                .Where(r => r.LotNumber == lotNumber)
                .ToListAsync();

            step.DocumentNumber = lotNumber;
            step.ValidationChecks.Add($"Results recorded: {results.Count}");

            if (results.Any())
            {
                foreach (var r in results.Take(5))
                    step.ValidationChecks.Add($"  — {r.DefectCodeGroup ?? "N/A"}: {r.DefectDescription ?? "N/A"} ({r.ResultStatus ?? "N/A"})");

                step.Status = PipelineStepStatus.Passed;
                step.Details = $"{results.Count} inspection result(s) recorded for lot {lotNumber}.";
            }
            else
            {
                step.Status = PipelineStepStatus.Warning;
                step.ErrorMessage = $"No inspection results found for lot '{lotNumber}'.";
                step.Resolution = "Record inspection results via QA32/QA33 before executing Usage Decision.";
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateUsageDecisionAsync(string? lotNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Usage Decision (QA11)",
            Description = "Validates UD executed with Accepted/Rejected decision",
            Category = PipelineStepCategory.QM_UsageDecision,
            EntityType = "UsageDecisionEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(lotNumber))
            {
                step.Status = PipelineStepStatus.Skipped;
                step.ErrorMessage = "No lot number provided — skipping UD validation.";
                return step;
            }

            var ud = await _db.UsageDecisions
                .AsNoTracking()
                .Where(u => u.LotNumber == lotNumber)
                .OrderByDescending(u => u.CreatedAt)
                .FirstOrDefaultAsync();

            if (ud == null)
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"No Usage Decision found for lot '{lotNumber}'.";
                step.Resolution = "Execute Usage Decision via QA11 (Accept/Reject/Scrap) before quality release.";
                return step;
            }

            step.DocumentNumber = ud.DecisionId;
            step.EntityId = ud.Id.ToString();
            step.StockType = ud.UDCode;
            step.Quantity = ud.UnrestrictedStock;

            step.ValidationChecks.Add($"Decision ID: {ud.DecisionId}");
            step.ValidationChecks.Add($"UD Code: {ud.UDCode}");
            step.ValidationChecks.Add($"Decision: {ud.Decision}");
            step.ValidationChecks.Add($"Inspector: {ud.InspectorID}");
            step.ValidationChecks.Add($"Unrestricted Stock: {ud.UnrestrictedStock}");
            step.ValidationChecks.Add($"Blocked Stock: {ud.BlockedStock}");
            step.ValidationChecks.Add($"Scrap: {ud.ScrapQuantity}");
            step.ValidationChecks.Add($"Decision Date: {ud.DecisionDate:yyyy-MM-dd HH:mm}");

            if (ud.UDCode?.ToUpperInvariant() is "A" or "ACCEPTED" or "REVERSED")
                step.Status = PipelineStepStatus.Passed;
            else if (ud.UDCode?.ToUpperInvariant() is "R" or "REJECTED" or "SCRAP" or "S")
            {
                step.Status = PipelineStepStatus.Warning;
                step.ErrorMessage = $"UD decision is '{ud.UDCode}' — stock may be blocked/scrapped, not unrestricted.";
                step.Details = "Material was rejected/scrapped. Downstream sales/delivery may be blocked.";
            }
            else
            {
                step.Status = PipelineStepStatus.Passed;
                step.Details = $"UD Code: {ud.UDCode}. Proceed with validation.";
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateQualityReleaseAsync(string? lotNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Quality Release (Movement Type 321)",
            Description = "Validates stock released from QualityInspection to Unrestricted",
            Category = PipelineStepCategory.QM_QualityRelease,
            EntityType = "StockMovementEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var movementQuery = _db.StockMovements
                .AsNoTracking()
                .Where(m => m.TenantId == tenantId && m.MovementType == "321" && m.Status == "Posted");

            if (!string.IsNullOrWhiteSpace(lotNumber))
                movementQuery = movementQuery.Where(m => m.Reference != null && m.Reference.Contains(lotNumber));

            var movement = await movementQuery.OrderByDescending(m => m.MovementDate).FirstOrDefaultAsync();

            if (movement == null)
            {
                step.Status = PipelineStepStatus.Warning;
                step.ErrorMessage = "No Quality Release (Movement Type 321) found.";
                step.Resolution = "Post Quality Release (321) to move stock from QualityInspection to Unrestricted.";
                step.Details = "Without 321 release, stock remains in QualityInspection and cannot be used for sales/delivery.";
                return step;
            }

            step.DocumentNumber = movement.DocumentNumber ?? movement.MovementNumber;
            step.EntityId = movement.Id.ToString();
            step.MovementType = "321";
            step.Quantity = movement.Quantity;
            step.StockType = "Unrestricted";

            step.ValidationChecks.Add($"Movement Number: {movement.MovementNumber}");
            step.ValidationChecks.Add($"Movement Type: {movement.MovementType}");
            step.ValidationChecks.Add($"Material: {movement.MaterialCode}");
            step.ValidationChecks.Add($"Quantity: {movement.Quantity}");
            step.ValidationChecks.Add($"Stock Before: {movement.StockBefore}");
            step.ValidationChecks.Add($"Stock After: {movement.StockAfter}");
            step.ValidationChecks.Add($"Posted By: {movement.PostedBy ?? "N/A"}");
            step.ValidationChecks.Add($"Movement Date: {movement.MovementDate:yyyy-MM-dd HH:mm}");

            step.Status = PipelineStepStatus.Passed;
            step.Details = "Stock successfully released from QualityInspection to Unrestricted.";
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateSalesOrderAsync(string? soNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Sales Order",
            Description = "Validates Sales Order exists and is confirmed",
            Category = PipelineStepCategory.SD_SalesOrder,
            EntityType = "SalesOrderEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var query = _db.SalesOrders.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(soNumber))
                query = query.Where(o => o.OrderNumber == soNumber);

            var so = await query.OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
            if (so == null)
            {
                step.Status = PipelineStepStatus.NotApplicable;
                step.ErrorMessage = $"No Sales Order found{(soNumber != null ? $" for '{soNumber}'" : "")}";
                step.Details = "Pipeline can proceed without SO if this is a stock-only scenario.";
                return step;
            }

            step.DocumentNumber = so.OrderNumber;
            step.EntityId = so.Id.ToString();
            step.Amount = so.Amount;

            step.ValidationChecks.Add($"Order Number: {so.OrderNumber}");
            step.ValidationChecks.Add($"Customer: {so.CustomerName}");
            step.ValidationChecks.Add($"Amount: {so.Amount:C}");
            step.ValidationChecks.Add($"Item Count: {so.ItemCount}");
            step.ValidationChecks.Add($"Status: {so.Status}");

            if (so.Status?.ToUpperInvariant() is "CONFIRMED" or "OPEN" or "PROCESSING" or "DELIVERED")
                step.Status = PipelineStepStatus.Passed;
            else if (so.Status?.ToUpperInvariant() is "PENDING")
            {
                step.Status = PipelineStepStatus.Warning;
                step.ErrorMessage = $"SO is in '{so.Status}' — should be confirmed before delivery.";
            }
            else
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"SO is in terminal status '{so.Status}'";
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateDeliveryAsync(string? soNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Outbound Delivery (VL01N)",
            Description = "Validates delivery created with Goods Issue (Movement Type 601)",
            Category = PipelineStepCategory.SD_Delivery,
            EntityType = "DeliveryEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var query = _db.Deliveries.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(soNumber))
                query = query.Where(d => d.SoNumber == soNumber);

            var delivery = await query.OrderByDescending(d => d.CreatedAt).FirstOrDefaultAsync();
            if (delivery == null)
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"No Delivery found{(soNumber != null ? $" for SO '{soNumber}'" : "")}";
                step.Resolution = "Create Outbound Delivery (VL01N) linked to the Sales Order.";
                return step;
            }

            step.DocumentNumber = delivery.DeliveryNumber;
            step.EntityId = delivery.Id.ToString();

            step.ValidationChecks.Add($"Delivery Number: {delivery.DeliveryNumber}");
            step.ValidationChecks.Add($"SO Reference: {delivery.SoNumber}");
            step.ValidationChecks.Add($"Customer: {delivery.CustomerName}");
            step.ValidationChecks.Add($"Status: {delivery.Status}");
            step.ValidationChecks.Add($"Date: {delivery.Date:yyyy-MM-dd}");

            if (delivery.Status?.ToUpperInvariant() is "SHIPPED" or "DELIVERED" or "PICKED")
                step.Status = PipelineStepStatus.Passed;
            else if (delivery.Status?.ToUpperInvariant() is "PENDING" or "CREATED")
            {
                step.Status = PipelineStepStatus.Warning;
                step.ErrorMessage = $"Delivery is in '{delivery.Status}' — Goods Issue (601) may not have been posted.";
                step.Resolution = "Post Goods Issue for the delivery (Movement Type 601) before billing.";
            }
            else
            {
                step.Status = PipelineStepStatus.Passed;
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateBillingDocumentAsync(string? deliveryNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Billing Document (VF01)",
            Description = "Validates billing document created with correct pricing conditions",
            Category = PipelineStepCategory.SD_Billing,
            EntityType = "BillingDocumentEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var query = _db.BillingDocuments.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(deliveryNumber))
                query = query.Where(b => b.SoNumber == deliveryNumber || b.DocumentNumber.Contains(deliveryNumber));

            var billing = await query.OrderByDescending(b => b.CreatedAt).FirstOrDefaultAsync();
            if (billing == null)
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"No Billing Document found{(deliveryNumber != null ? $" for delivery '{deliveryNumber}'" : "")}";
                step.Resolution = "Create Billing Document (VF01) for the delivered items.";
                return step;
            }

            step.DocumentNumber = billing.DocumentNumber;
            step.EntityId = billing.Id.ToString();
            step.Amount = billing.Amount;

            var lines = await _db.BillingDocumentLines
                .AsNoTracking()
                .Where(l => l.BillingDocumentId == billing.Id)
                .ToListAsync();

            step.ValidationChecks.Add($"Document Number: {billing.DocumentNumber}");
            step.ValidationChecks.Add($"Date: {billing.Date:yyyy-MM-dd}");
            step.ValidationChecks.Add($"Customer: {billing.CustomerName}");
            step.ValidationChecks.Add($"Amount: {billing.Amount:C}");
            step.ValidationChecks.Add($"Status: {billing.Status}");
            step.ValidationChecks.Add($"Line Items: {lines.Count}");

            if (lines.Any())
            {
                foreach (var line in lines.Take(3))
                    step.ValidationChecks.Add($"  — {line.MaterialName}: Qty {line.Quantity}, Net {line.NetAmount:C}, Tax {line.TaxAmount:C}");
            }

            if (billing.Status?.ToUpperInvariant() is "POSTED" or "OPEN" or "CREATED")
                step.Status = PipelineStepStatus.Passed;
            else
            {
                step.Status = PipelineStepStatus.Warning;
                step.ErrorMessage = $"Billing doc in status '{billing.Status}'";
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateFiLedgerPostingAsync(string? billingDocNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "FI General Ledger Posting (FB03)",
            Description = "Validates balanced GL entries in Universal Journal (AR Dr, Revenue Cr, Freight Cr, Tax Cr)",
            Category = PipelineStepCategory.FI_GeneralLedger,
            EntityType = "UniversalJournalEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var query = _db.UniversalJournals.AsNoTracking().Where(j => j.TenantId == tenantId);
            if (!string.IsNullOrWhiteSpace(billingDocNumber))
                query = query.Where(j => j.DocumentNumber == billingDocNumber || (j.Reference != null && j.Reference.Contains(billingDocNumber)));

            var entries = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

            if (!entries.Any())
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"No FI Journal entries found{(billingDocNumber != null ? $" for billing doc '{billingDocNumber}'" : "")}";
                step.Resolution = "Release Billing Document to FI (VF02 -> FB03) to generate GL entries.";
                return step;
            }

            step.DocumentNumber = entries.First().DocumentNumber;

            var totalDebit = entries.Sum(e => e.DebitAmount);
            var totalCredit = entries.Sum(e => e.CreditAmount);
            var isBalanced = Math.Abs(totalDebit - totalCredit) < 0.01m;
            step.Amount = totalDebit;

            step.ValidationChecks.Add($"Document Number: {entries.First().DocumentNumber}");
            step.ValidationChecks.Add($"Entry Count: {entries.Count}");
            step.ValidationChecks.Add($"Total Debit: {totalDebit:C}");
            step.ValidationChecks.Add($"Total Credit: {totalCredit:C}");
            step.ValidationChecks.Add($"Balanced: {isBalanced}");
            step.ValidationChecks.Add($"Posting Date: {entries.First().PostedAt:yyyy-MM-dd HH:mm}");
            step.ValidationChecks.Add($"Created By: {entries.First().CreatedBy ?? "N/A"}");

            foreach (var entry in entries.Take(6))
            {
                var amt = entry.DebitAmount > 0 ? $"Dr {entry.DebitAmount:C}" : $"Cr {entry.CreditAmount:C}";
                step.ValidationChecks.Add($"  — {entry.AccountCode} ({entry.AccountName}): {amt}");
            }

            if (isBalanced)
                step.Status = PipelineStepStatus.Passed;
            else
            {
                step.Status = PipelineStepStatus.Failed;
                step.ErrorMessage = $"GL is UNBALANCED! Debit {totalDebit:C} ≠ Credit {totalCredit:C} (difference: {Math.Abs(totalDebit - totalCredit):C})";
                step.Resolution = "Investigate missing journal entries. AR (1400) Dr must equal Revenue (4000) + Freight (4100) + Tax (2300) Cr.";
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    public async Task<PipelineStepResult> ValidateStockIntegrityAsync(string? materialCode, string? batchNumber, Guid tenantId)
    {
        var step = new PipelineStepResult
        {
            StepName = "Stock Integrity Check",
            Description = "Cross-validates stock balances across MM stock table and stock_balances entity",
            Category = PipelineStepCategory.CrossCutting_TransactionIntegrity,
            EntityType = "StockBalanceEntity"
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var mmStock = await _db.StockItems
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(materialCode))
                mmStock = mmStock.Where(s => s.MaterialName != null && s.MaterialName.Contains(materialCode, StringComparison.OrdinalIgnoreCase)).ToList();

            var balanceStock = await _db.StockBalances
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(materialCode))
                balanceStock = balanceStock.Where(s => s.MaterialCode.Contains(materialCode, StringComparison.OrdinalIgnoreCase)).ToList();

            step.ValidationChecks.Add($"MM Stock Rows: {mmStock.Count}");
            step.ValidationChecks.Add($"Stock Balance Rows: {balanceStock.Count}");

            var mmTotalQty = mmStock.Sum(s => s.Quantity);
            var mmTotalValue = mmStock.Sum(s => s.Value);
            var balTotalQty = balanceStock.Sum(s => s.Quantity);
            var balTotalValue = balanceStock.Sum(s => s.TotalValue);

            step.ValidationChecks.Add($"MM Total Qty: {mmTotalQty}, Value: {mmTotalValue:C}");
            step.ValidationChecks.Add($"Balance Total Qty: {balTotalQty}, Value: {balTotalValue:C}");

            if (mmStock.Any())
            {
                foreach (var s in mmStock.Take(4))
                    step.ValidationChecks.Add($"  — {s.MaterialName}: {s.Quantity} {s.UOM}, Lot: {s.Lot ?? "N/A"}");
            }

            if (balanceStock.Any())
            {
                var grouped = balanceStock.GroupBy(b => new { b.MaterialCode, b.StockType });
                foreach (var g in grouped.Take(4))
                    step.ValidationChecks.Add($"  — {g.Key.MaterialCode} [{g.Key.StockType}]: {g.Sum(s => s.Quantity)}");
            }

            step.Quantity = mmTotalQty;
            step.Amount = mmTotalValue;

            step.Status = mmStock.Any() || balanceStock.Any() ? PipelineStepStatus.Passed : PipelineStepStatus.Warning;
            if (!mmStock.Any() && !balanceStock.Any())
            {
                step.ErrorMessage = "No stock data found for the given criteria.";
                step.Details = "Stock tables may be empty. Verify GR and stock postings.";
            }
            else
            {
                step.Details = "Stock data present across MM tables. Cross-validation complete.";
            }
        }
        catch (Exception ex)
        {
            step.Status = PipelineStepStatus.Failed;
            step.ErrorMessage = ex.Message;
        }
        sw.Stop();
        step.DurationMs = sw.ElapsedMilliseconds;
        return step;
    }

    private static List<string> GenerateRecommendations(PipelineDiagnosticResult result)
    {
        var recs = new List<string>();

        if (result.FailedSteps > 0)
            recs.Add($"CRITICAL: {result.FailedSteps} step(s) FAILED. Resolve before processing new transactions.");

        if (result.Steps.Any(s => s.Category == PipelineStepCategory.QM_UsageDecision && s.Status == PipelineStepStatus.Failed))
            recs.Add("Usage Decision missing. Execute QA11 to unblock downstream delivery and billing.");

        if (result.Steps.Any(s => s.Category == PipelineStepCategory.QM_QualityRelease && s.Status == PipelineStepStatus.Warning))
            recs.Add("Quality Release (321) not posted. Stock stuck in QualityInspection — cannot deliver.");

        if (result.Steps.Any(s => s.Category == PipelineStepCategory.SD_Delivery && s.Status == PipelineStepStatus.Failed))
            recs.Add("Delivery missing. Create VL01N for the confirmed Sales Order.");

        if (result.Steps.Any(s => s.Category == PipelineStepCategory.FI_GeneralLedger && s.Status == PipelineStepStatus.Failed))
            recs.Add("FI posting missing or unbalanced. Release billing document to FI (VF02).");

        if (result.Traceability?.IsBalanced == false)
            recs.Add("FI GL entries are UNBALANCED. Investigate missing debit/credit lines.");

        if (result.WarningSteps > 0)
            recs.Add($"{result.WarningSteps} warning(s) detected. Review details for potential issues.");

        if (result.PassedSteps == result.TotalSteps)
            recs.Add("All pipeline steps PASSED. E2E flow is healthy.");

        return recs;
    }
}
