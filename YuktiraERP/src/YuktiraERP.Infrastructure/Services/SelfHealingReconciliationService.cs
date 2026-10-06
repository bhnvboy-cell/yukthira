using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class SelfHealingReconciliationService : ISelfHealingReconciliationService
{
    private const string GenesisSeed = "GENESIS";
    private const string GrIrMarker = "GR/IR";

    private static readonly HashSet<string> PostedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Posted",
        "Completed",
        "Reversed",
        "Resolved"
    };

    private readonly YuktiraDbContext _db;
    private readonly IInventoryMovementService _movementService;
    private readonly IAuditService _auditService;
    private readonly SelfHealingOptions _options;

    public SelfHealingReconciliationService(
        YuktiraDbContext db,
        IInventoryMovementService movementService,
        IAuditService auditService,
        IOptions<SelfHealingOptions> options)
    {
        _db = db;
        _movementService = movementService;
        _auditService = auditService;
        _options = options?.Value ?? new SelfHealingOptions();
    }

    public async Task<SelfHealingRunResultDto> RunOnceAsync(Guid tenantId, string userId, CancellationToken cancellationToken = default)
    {
        var result = new SelfHealingRunResultDto
        {
            TenantId = tenantId,
            StartedAt = DateTime.UtcNow
        };

        var lastAudit = await _db.SxAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = lastAudit?.SequenceNumber ?? 0;
        var previousHash = string.IsNullOrEmpty(lastAudit?.CurrentHash) ? GenesisSeed : lastAudit!.CurrentHash;

        await DetectAndFixGrIrAsync(tenantId, userId, sequence, previousHash, result, cancellationToken);
        previousHash = result.Actions.Count > 0 ? result.Actions[^1].CurrentHash : previousHash;
        sequence = result.Actions.Count > 0 ? result.Actions[^1].SequenceNumber : sequence;

        await DetectAndFixPennyAsync(tenantId, userId, sequence, previousHash, result, cancellationToken);
        previousHash = result.Actions.Count > 0 ? result.Actions[^1].CurrentHash : previousHash;
        sequence = result.Actions.Count > 0 ? result.Actions[^1].SequenceNumber : sequence;

        await DetectAndRepairStuckPostingsAsync(tenantId, userId, sequence, previousHash, result, cancellationToken);

        result.FinishedAt = DateTime.UtcNow;
        return result;
    }

    private async Task DetectAndFixGrIrAsync(
        Guid tenantId,
        string userId,
        long sequence,
        string previousHash,
        SelfHealingRunResultDto result,
        CancellationToken ct)
    {
        var chain = new ChainState { Sequence = sequence, PreviousHash = previousHash };
        var lines = await _db.UniversalJournals.AsNoTracking()
            .Where(j => j.TenantId == tenantId
                && (j.AccountName.ToUpper().Contains(GrIrMarker)
                    || j.AccountCode.ToUpper().Contains(GrIrMarker)))
            .ToListAsync(ct);

        result.GrIrScanned = lines.Count;

        var groups = lines
            .GroupBy(j => new
            {
                j.VendorCode,
                j.MaterialCode,
                GroupKey = string.IsNullOrEmpty(j.Reference) ? j.DocumentNumber : j.Reference
            })
            .ToList();

        foreach (var group in groups)
        {
            var debit = group.Sum(x => x.DebitAmount);
            var credit = group.Sum(x => x.CreditAmount);
            var delta = debit - credit;
            var sample = group.First();
            var targetId = $"{sample.VendorCode}|{sample.MaterialCode}|{group.Key.GroupKey}";

            if (delta == 0m)
            {
                await RecordActionAsync(
                    tenantId, userId, chain, result,
                    category: "GrIrReconciliation",
                    targetEntity: "UniversalJournal",
                    targetId: targetId,
                    summary: $"GR/IR group {targetId} is balanced (Dr={debit}, Cr={credit})",
                    deltaAmount: 0m,
                    currency: sample.Currency,
                    status: "Skipped",
                    details: JsonSerializer.Serialize(new { vendorCode = sample.VendorCode, materialCode = sample.MaterialCode, debit, credit }));
                continue;
            }

            if (Math.Abs(delta) > _options.MaxAutoFixAmount)
            {
                await RecordActionAsync(
                    tenantId, userId, chain, result,
                    category: "GrIrReconciliation",
                    targetEntity: "UniversalJournal",
                    targetId: targetId,
                    summary: $"GR/IR group {targetId} delta {delta} exceeds MaxAutoFixAmount {_options.MaxAutoFixAmount}",
                    deltaAmount: delta,
                    currency: sample.Currency,
                    status: "Flagged",
                    details: JsonSerializer.Serialize(new { vendorCode = sample.VendorCode, materialCode = sample.MaterialCode, debit, credit, delta, maxAutoFixAmount = _options.MaxAutoFixAmount }));
                continue;
            }

            try
            {
                var docNumber = $"SHGIR-{sample.DocumentNumber}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                var balancing = new UniversalJournalEntity
                {
                    TenantId = tenantId,
                    FiscalYear = sample.FiscalYear,
                    Period = sample.Period,
                    DocumentNumber = docNumber,
                    DocumentType = string.IsNullOrEmpty(sample.DocumentType) ? "SA" : sample.DocumentType,
                    DocumentDate = DateTime.UtcNow,
                    PostingDate = DateTime.UtcNow,
                    LineNumber = 1,
                    AccountCode = sample.AccountCode,
                    AccountName = sample.AccountName,
                    AccountType = sample.AccountType,
                    DebitAmount = delta < 0 ? Math.Abs(delta) : 0m,
                    CreditAmount = delta > 0 ? delta : 0m,
                    Currency = string.IsNullOrEmpty(sample.Currency) ? "INR" : sample.Currency,
                    AmountLC = delta < 0 ? Math.Abs(delta) : -delta,
                    CostCenter = sample.CostCenter,
                    ProfitCenter = sample.ProfitCenter,
                    Plant = sample.Plant,
                    MaterialCode = sample.MaterialCode,
                    VendorCode = sample.VendorCode,
                    Reference = sample.Reference,
                    Description = "Self-healing GR/IR reconciliation",
                    Status = "Posted",
                    PostedAt = DateTime.UtcNow,
                    CreatedBy = "SELF_HEALING",
                    Hash = ComputeSha256($"{docNumber}|{sample.AccountCode}|{delta}|{DateTime.UtcNow:O}")
                };
                _db.UniversalJournals.Add(balancing);
                await _db.SaveChangesAsync(ct);

                await RecordActionAsync(
                    tenantId, userId, chain, result,
                    category: "GrIrReconciliation",
                    targetEntity: "UniversalJournal",
                    targetId: targetId,
                    summary: $"Posted balancing GR/IR line for {targetId} (delta {delta})",
                    deltaAmount: delta,
                    currency: balancing.Currency,
                    status: "Applied",
                    details: JsonSerializer.Serialize(new { documentNumber = docNumber, vendorCode = sample.VendorCode, materialCode = sample.MaterialCode, debit, credit, delta }));
            }
            catch (Exception ex)
            {
                await RecordActionAsync(
                    tenantId, userId, chain, result,
                    category: "GrIrReconciliation",
                    targetEntity: "UniversalJournal",
                    targetId: targetId,
                    summary: $"Failed to post balancing GR/IR line for {targetId}: {ex.Message}",
                    deltaAmount: delta,
                    currency: sample.Currency,
                    status: "Failed",
                    details: JsonSerializer.Serialize(new { error = ex.Message, debit, credit, delta }));
            }
        }
    }

    private async Task DetectAndFixPennyAsync(
        Guid tenantId,
        string userId,
        long sequence,
        string previousHash,
        SelfHealingRunResultDto result,
        CancellationToken ct)
    {
        var chain = new ChainState { Sequence = sequence, PreviousHash = previousHash };
        var lines = await _db.UniversalJournals.AsNoTracking()
            .Where(j => j.TenantId == tenantId)
            .ToListAsync(ct);

        result.PennyScanned = lines.Count;

        var docGroups = lines
            .GroupBy(j => j.DocumentNumber)
            .Select(g => new
            {
                DocumentNumber = g.Key,
                Lines = g.ToList(),
                Delta = g.Sum(x => x.DebitAmount) - g.Sum(x => x.CreditAmount)
            })
            .Where(g => g.Delta != 0m && Math.Abs(g.Delta) < _options.PennyTolerance)
            .ToList();

        foreach (var doc in docGroups)
        {
            var sample = doc.Lines.First();
            try
            {
                var docNumber = $"SHPEN-{doc.DocumentNumber}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                var rounding = new UniversalJournalEntity
                {
                    TenantId = tenantId,
                    FiscalYear = sample.FiscalYear,
                    Period = sample.Period,
                    DocumentNumber = docNumber,
                    DocumentType = string.IsNullOrEmpty(sample.DocumentType) ? "SA" : sample.DocumentType,
                    DocumentDate = DateTime.UtcNow,
                    PostingDate = DateTime.UtcNow,
                    LineNumber = 1,
                    AccountCode = "49999",
                    AccountName = "Rounding Off",
                    AccountType = "Expense",
                    DebitAmount = doc.Delta < 0 ? Math.Abs(doc.Delta) : 0m,
                    CreditAmount = doc.Delta > 0 ? doc.Delta : 0m,
                    Currency = string.IsNullOrEmpty(sample.Currency) ? "INR" : sample.Currency,
                    AmountLC = doc.Delta < 0 ? Math.Abs(doc.Delta) : -doc.Delta,
                    Reference = sample.Reference,
                    Description = $"Penny rounding adjustment for document {doc.DocumentNumber}",
                    Status = "Posted",
                    PostedAt = DateTime.UtcNow,
                    CreatedBy = "SELF_HEALING",
                    Hash = ComputeSha256($"{docNumber}|49999|{doc.Delta}|{DateTime.UtcNow:O}")
                };
                _db.UniversalJournals.Add(rounding);
                await _db.SaveChangesAsync(ct);

                await RecordActionAsync(
                    tenantId, userId, chain, result,
                    category: "PennyRounding",
                    targetEntity: "UniversalJournal",
                    targetId: doc.DocumentNumber,
                    summary: $"Applied rounding line 49999 for document {doc.DocumentNumber} (delta {doc.Delta})",
                    deltaAmount: doc.Delta,
                    currency: rounding.Currency,
                    status: "Applied",
                    details: JsonSerializer.Serialize(new { documentNumber = docNumber, sourceDocument = doc.DocumentNumber, delta = doc.Delta, tolerance = _options.PennyTolerance }));
            }
            catch (Exception ex)
            {
                await RecordActionAsync(
                    tenantId, userId, chain, result,
                    category: "PennyRounding",
                    targetEntity: "UniversalJournal",
                    targetId: doc.DocumentNumber,
                    summary: $"Failed to apply penny rounding for {doc.DocumentNumber}: {ex.Message}",
                    deltaAmount: doc.Delta,
                    currency: sample.Currency,
                    status: "Failed",
                    details: JsonSerializer.Serialize(new { error = ex.Message, delta = doc.Delta }));
            }
        }
    }

    private async Task DetectAndRepairStuckPostingsAsync(
        Guid tenantId,
        string userId,
        long sequence,
        string previousHash,
        SelfHealingRunResultDto result,
        CancellationToken ct)
    {
        var chain = new ChainState { Sequence = sequence, PreviousHash = previousHash };
        var tenantKey = tenantId.ToString();
        var cutoff = DateTime.UtcNow.AddMinutes(-Math.Max(1, _options.StuckPostingAgeMinutes));

        var stuck = await _db.MaterialDocumentHeaders.AsNoTracking()
            .Where(h => h.TenantId == tenantKey
                && (h.MovementType == 322 || h.MovementType == 324)
                && !PostedStatuses.Contains(h.Status)
                && h.CreatedAt < cutoff)
            .ToListAsync(ct);

        result.StuckPostingsScanned = stuck.Count;

        foreach (var header in stuck)
        {
            var hasHistory = await _db.StockMovementHistory.AsNoTracking()
                .AnyAsync(h => h.TenantId == tenantKey && h.DocumentNumber == header.DocumentNumber, ct);

            var items = await _db.MaterialDocumentItems.AsNoTracking()
                .Where(i => i.MaterialDocumentHeaderId == header.Id.ToString() && i.TenantId == tenantKey)
                .ToListAsync(ct);

            var safeRetry = !hasHistory && items.Count > 0;

            if (safeRetry)
            {
                try
                {
                    var retry = await _movementService.PostTransferPostingAsync(new Core.Dtos.PostGoodsMovementRequestDto
                    {
                        MovementType = header.MovementType,
                        PostingDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        DocumentDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        HeaderText = $"Self-healing re-post of {header.DocumentNumber}",
                        Reference = header.DocumentNumber,
                        Lines = items.Select(i => new Core.Dtos.PostGoodsMovementLineDto
                        {
                            MaterialCode = i.MaterialCode,
                            MaterialName = i.MaterialName,
                            Plant = i.Plant,
                            StorageLocation = i.StorageLocation,
                            BatchNumber = i.BatchNumber,
                            Quantity = i.Quantity,
                            UOM = i.UnitOfMeasure,
                            UnitPrice = i.UnitPrice,
                            VendorCode = i.VendorCode,
                            CustomerCode = i.CustomerCode
                        }).ToList()
                    }, tenantId, string.IsNullOrEmpty(userId) ? "SELF_HEALING" : userId);

                    if (retry.Success)
                    {
                        header.Status = "Resolved";
                        header.UpdatedAt = DateTime.UtcNow;
                        _db.MaterialDocumentHeaders.Update(header);
                        await _db.SaveChangesAsync(ct);

                        await RecordActionAsync(
                            tenantId, userId, chain, result,
                            category: "StuckPosting",
                            targetEntity: "MaterialDocumentHeader",
                            targetId: header.DocumentNumber,
                            summary: $"Re-posted stuck {header.MovementType} document {header.DocumentNumber} as {retry.DocumentNumber}",
                            deltaAmount: retry.TotalValue,
                            currency: "INR",
                            status: "Applied",
                            details: JsonSerializer.Serialize(new { originalDocument = header.DocumentNumber, newDocument = retry.DocumentNumber, movementType = header.MovementType }));
                    }
                    else
                    {
                        header.Status = "Resolved";
                        header.UpdatedAt = DateTime.UtcNow;
                        _db.MaterialDocumentHeaders.Update(header);
                        await _db.SaveChangesAsync(ct);

                        await RecordActionAsync(
                            tenantId, userId, chain, result,
                            category: "StuckPosting",
                            targetEntity: "MaterialDocumentHeader",
                            targetId: header.DocumentNumber,
                            summary: $"Stuck {header.MovementType} document {header.DocumentNumber} re-post failed: {string.Join("; ", retry.Errors)}",
                            deltaAmount: 0m,
                            currency: "INR",
                            status: "Failed",
                            details: JsonSerializer.Serialize(new { errors = retry.Errors, movementType = header.MovementType }));
                    }
                }
                catch (Exception ex)
                {
                    await RecordActionAsync(
                        tenantId, userId, chain, result,
                        category: "StuckPosting",
                        targetEntity: "MaterialDocumentHeader",
                        targetId: header.DocumentNumber,
                        summary: $"Exception while repairing stuck document {header.DocumentNumber}: {ex.Message}",
                        deltaAmount: 0m,
                        currency: "INR",
                        status: "Failed",
                        details: JsonSerializer.Serialize(new { error = ex.Message, movementType = header.MovementType }));
                }
            }
            else
            {
                header.Status = "Resolved";
                header.UpdatedAt = DateTime.UtcNow;
                _db.MaterialDocumentHeaders.Update(header);
                await _db.SaveChangesAsync(ct);

                await RecordActionAsync(
                    tenantId, userId, chain, result,
                    category: "StuckPosting",
                    targetEntity: "MaterialDocumentHeader",
                    targetId: header.DocumentNumber,
                    summary: $"Stuck {header.MovementType} document {header.DocumentNumber} resolved and flagged for review (safe retry unavailable)",
                    deltaAmount: 0m,
                    currency: "INR",
                    status: "Flagged",
                    details: JsonSerializer.Serialize(new { movementType = header.MovementType, hasStockHistory = hasHistory, itemCount = items.Count, statusBefore = header.Status }));
            }
        }
    }

    private sealed class ChainState
    {
        public long Sequence;
        public string PreviousHash = "";
    }

    private async Task RecordActionAsync(
        Guid tenantId,
        string userId,
        ChainState chain,
        SelfHealingRunResultDto result,
        string category,
        string targetEntity,
        string targetId,
        string summary,
        decimal deltaAmount,
        string currency,
        string status,
        string details)
    {
        chain.Sequence += 1;
        var payload = $"{chain.Sequence}|{category}|{targetId}|{summary}|{deltaAmount}|{chain.PreviousHash}";
        var currentHash = ComputeSha256(payload);

        var auditRow = new SxAuditEntity
        {
            TenantId = tenantId,
            SequenceNumber = chain.Sequence,
            ActionCategory = category,
            TargetEntity = targetEntity,
            TargetId = targetId,
            Summary = summary,
            DeltaAmount = deltaAmount,
            Currency = string.IsNullOrEmpty(currency) ? "INR" : currency,
            Status = status,
            UserId = string.IsNullOrEmpty(userId) ? "system" : userId,
            PreviousHash = chain.PreviousHash,
            CurrentHash = currentHash,
            Details = details
        };
        _db.SxAudits.Add(auditRow);
        await _db.SaveChangesAsync();

        var action = new SelfHealingActionDto
        {
            Category = category,
            TargetEntity = targetEntity,
            TargetId = targetId,
            Summary = summary,
            DeltaAmount = deltaAmount,
            Currency = auditRow.Currency,
            Status = status,
            UserId = auditRow.UserId,
            Details = details,
            SequenceNumber = chain.Sequence,
            PreviousHash = chain.PreviousHash,
            CurrentHash = currentHash
        };
        result.Actions.Add(action);

        switch (status)
        {
            case "Applied":
                result.AppliedCount++;
                break;
            case "Skipped":
                result.SkippedCount++;
                break;
            case "Flagged":
                result.FlaggedCount++;
                break;
            case "Failed":
                result.FailedCount++;
                break;
        }

        chain.PreviousHash = currentHash;

        try
        {
            await _auditService.LogAsync(new AuditEntryDto
            {
                Timestamp = DateTime.UtcNow,
                UserId = Guid.TryParse(userId, out var uid) ? uid : null,
                TenantId = tenantId,
                ModuleName = "SelfHealing",
                ActionType = status == "Applied" ? ActionType.Update : ActionType.Config,
                EntityName = targetEntity,
                EntityId = targetId,
                Details = $"{status}: {summary}"
            });
        }
        catch
        {
        }
    }

    private static string ComputeSha256(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
