using Microsoft.EntityFrameworkCore;
using YuktiraERP.Mes.Core.Domain.Entities;
using YuktiraERP.Mes.Core.Domain.Enums;
using YuktiraERP.Mes.Infrastructure.Data;

namespace YuktiraERP.Mes.Infrastructure.Services;

/// <summary>
/// Manages MES-to-ERP posting logs and the Exception Cockpit retry/reconciliation queue (Section 13.1 & 13.2).
/// </summary>
public class MesErpPostingBridge
{
    private readonly MesDbContext _db;

    public MesErpPostingBridge(MesDbContext db) => _db = db;

    public async Task<IReadOnlyList<MesErpPostingLog>> GetExceptionsAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.ErpPostingLogs
            .Where(l => l.TenantId == tenantId && l.PostingStatus != PostingStatus.POSTED)
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

    public async Task<MesErpPostingLog> MarkPostedAsync(Guid tenantId, Guid postingLogId, string erpDocNo, CancellationToken ct = default)
    {
        var log = await _db.ErpPostingLogs.FirstOrDefaultAsync(l => l.Id == postingLogId && l.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Posting log {postingLogId} not found.");

        log.PostingStatus = PostingStatus.POSTED;
        log.ErpDocNo = erpDocNo;
        log.PostedAt = DateTime.UtcNow;
        log.LastError = null;
        await _db.SaveChangesAsync(ct);
        return log;
    }

    public async Task<MesErpPostingLog> RetryPostingAsync(Guid tenantId, Guid postingLogId, CancellationToken ct = default)
    {
        var log = await _db.ErpPostingLogs.FirstOrDefaultAsync(l => l.Id == postingLogId && l.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Posting log {postingLogId} not found.");

        log.PostingStatus = PostingStatus.PENDING;
        log.AttemptCount++;
        await _db.SaveChangesAsync(ct);
        return log;
    }
}
