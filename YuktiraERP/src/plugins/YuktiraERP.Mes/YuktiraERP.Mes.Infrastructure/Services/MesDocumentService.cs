using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Common;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

public class MesDocumentService
{
    private readonly MesDbContext _db;

    public MesDocumentService(MesDbContext db) => _db = db;

    public async Task<(MesDocument Doc, MesDocumentRevision Rev)> CreateDocumentRevisionAsync(
        Guid tenantId, string plantCode, string docNo, MesDocType docType, string title,
        Guid ownerId, string revision, string fileHash, string storageUri, CancellationToken ct = default)
    {
        var doc = await _db.Documents.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DocNo == docNo, ct);
        if (doc == null)
        {
            doc = new MesDocument
            {
                TenantId = tenantId,
                PlantCode = plantCode,
                DocNo = docNo,
                DocType = docType,
                Title = title,
                OwnerId = ownerId
            };
            _db.Documents.Add(doc);
        }

        var rev = new MesDocumentRevision
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            DocId = doc.Id,
            Revision = revision,
            Status = DocRevisionStatus.DRAFT,
            FileHash = fileHash,
            StorageUri = storageUri
        };
        rev.RecordHash = RecordHashHelper.ComputeSha256(rev.Id, doc.Id, revision, fileHash, storageUri, rev.CreatedAt);
        _db.DocumentRevisions.Add(rev);

        await _db.SaveChangesAsync(ct);
        return (doc, rev);
    }

    public async Task<MesDocumentRevision> MakeEffectiveAsync(Guid tenantId, Guid revId, CancellationToken ct = default)
    {
        var rev = await _db.DocumentRevisions.FirstOrDefaultAsync(r => r.Id == revId && r.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Document revision {revId} not found.");

        var priorEffective = await _db.DocumentRevisions
            .Where(r => r.TenantId == tenantId && r.DocId == rev.DocId && r.Status == DocRevisionStatus.EFFECTIVE && r.Id != revId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var old in priorEffective)
        {
            old.Status = DocRevisionStatus.SUPERSEDED;
            old.EffectiveTo = now;
        }

        rev.Status = DocRevisionStatus.EFFECTIVE;
        rev.EffectiveFrom = now;
        await _db.SaveChangesAsync(ct);
        return rev;
    }

    public async Task<MesDocumentRevision> RetrieveVerifiedRevisionAsync(Guid tenantId, Guid revId, string observedFileHash, CancellationToken ct = default)
    {
        var rev = await _db.DocumentRevisions.FirstOrDefaultAsync(r => r.Id == revId && r.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Document revision {revId} not found.");

        if (!string.Equals(rev.FileHash, observedFileHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Security event: file hash mismatch on revision {rev.Revision}. Display refused.");

        return rev;
    }

    public async Task<MesESignature> SignAsync(
        Guid tenantId, string plantCode, Guid userId, ESignatureMeaning meaning, string authMethod = "PASSWORD_MFA", CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var sig = new MesESignature
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            UserId = userId,
            Meaning = meaning,
            SignedAt = now,
            AuthMethod = authMethod
        };
        sig.RecordHash = RecordHashHelper.ComputeSha256(sig.Id, userId, meaning, now, authMethod);
        _db.ESignatures.Add(sig);
        await _db.SaveChangesAsync(ct);
        return sig;
    }

    public async Task<MesEbrStepEntry> RecordEbrStepAsync(
        Guid tenantId, string plantCode, Guid ebrId, int stepNo,
        string value, string uom, decimal? lowLimit, decimal? highLimit,
        string enteredBy, Guid? esignId = null, CancellationToken ct = default)
    {
        var deviation = false;
        if (decimal.TryParse(value, out var numVal))
        {
            if (lowLimit.HasValue && numVal < lowLimit.Value) deviation = true;
            if (highLimit.HasValue && numVal > highLimit.Value) deviation = true;
        }

        var entry = new MesEbrStepEntry
        {
            TenantId = tenantId,
            PlantCode = plantCode,
            EbrId = ebrId,
            StepNo = stepNo,
            Value = value,
            Uom = uom,
            LowLimit = lowLimit,
            HighLimit = highLimit,
            DeviationFlag = deviation,
            EnteredBy = enteredBy,
            EnteredAt = DateTime.UtcNow,
            EsignId = esignId
        };
        _db.EbrStepEntries.Add(entry);

        if (deviation)
        {
            var hold = new MesHold
            {
                TenantId = tenantId,
                PlantCode = plantCode,
                TargetType = HoldTargetType.WORK_ORDER,
                TargetId = ebrId,
                Reason = $"EBR step {stepNo} deviation: value={value} {uom}",
                PlacedBy = "SYSTEM",
                PlacedAt = DateTime.UtcNow,
                Status = HoldStatus.ACTIVE
            };
            hold.RecordHash = RecordHashHelper.ComputeSha256(hold.Id, hold.TargetType, hold.TargetId, hold.Reason, hold.PlacedAt);
            _db.Holds.Add(hold);
        }

        await _db.SaveChangesAsync(ct);
        return entry;
    }
}
