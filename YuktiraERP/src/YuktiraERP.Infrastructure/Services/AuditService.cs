using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly YuktiraDbContext _db;

    public AuditService(YuktiraDbContext db) { _db = db; }

    public async Task LogAsync(AuditEntryDto entry)
    {
        _db.AuditLogs.Add(new AuditLogEntity
        {
            TenantId = entry.TenantId,
            UserId = entry.UserId,
            UserName = "",
            ModuleName = entry.ModuleName,
            EntityName = entry.EntityName,
            ActionType = entry.ActionType.ToString(),
            Description = entry.Details ?? $"{entry.ActionType} {entry.EntityName}",
            OldValues = entry.OldValue ?? "",
            NewValues = entry.NewValue ?? "",
            IpAddress = entry.IpAddress ?? "",
            DeviceInfo = entry.DeviceInfo ?? "",
            UserAgent = entry.UserAgent ?? "",
            SessionId = entry.SessionId ?? "",
            IsFlagged = entry.IsSuspicious,
            Timestamp = entry.Timestamp
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditEntryDto>> GetLogsAsync(Guid? tenantId, Guid? userId, string? module, ActionType? actionType, DateTime? from, DateTime? to, int page = 1, int pageSize = 50)
    {
        var query = _db.AuditLogs.AsQueryable();
        if (tenantId.HasValue) query = query.Where(a => a.TenantId == tenantId);
        if (userId.HasValue) query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrEmpty(module)) query = query.Where(a => a.ModuleName == module);
        if (actionType.HasValue) query = query.Where(a => a.ActionType == actionType.ToString());
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

        var logs = await query.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return logs.Select(a => new AuditEntryDto
        {
            Timestamp = a.Timestamp,
            UserId = a.UserId,
            TenantId = a.TenantId,
            ModuleName = a.ModuleName,
            ActionType = Enum.TryParse<ActionType>(a.ActionType, out var at) ? at : ActionType.ApiCall,
            EntityName = a.EntityName,
            OldValue = a.OldValues,
            NewValue = a.NewValues,
            IpAddress = a.IpAddress,
            DeviceInfo = a.DeviceInfo,
            UserAgent = a.UserAgent,
            SessionId = a.SessionId,
            IsSuspicious = a.IsFlagged,
            Details = a.Description
        }).ToList();
    }

    public async Task<AuditEntryDto?> GetLogByIdAsync(Guid id)
    {
        var a = await _db.AuditLogs.FindAsync(id);
        if (a == null) return null;
        return new AuditEntryDto
        {
            Timestamp = a.Timestamp,
            UserId = a.UserId,
            TenantId = a.TenantId,
            ModuleName = a.ModuleName,
            ActionType = Enum.TryParse<ActionType>(a.ActionType, out var at) ? at : ActionType.ApiCall,
            EntityName = a.EntityName,
            OldValue = a.OldValues,
            NewValue = a.NewValues,
            IpAddress = a.IpAddress,
            DeviceInfo = a.DeviceInfo,
            UserAgent = a.UserAgent,
            SessionId = a.SessionId,
            IsSuspicious = a.IsFlagged,
            Details = a.Description
        };
    }

    public async Task<int> GetLogCountAsync(Guid? tenantId)
    {
        var query = _db.AuditLogs.AsQueryable();
        if (tenantId.HasValue) query = query.Where(a => a.TenantId == tenantId);
        return await query.CountAsync();
    }

    public async Task ExportToCsvAsync(Guid? tenantId, Stream stream)
    {
        var query = _db.AuditLogs.AsQueryable();
        if (tenantId.HasValue) query = query.Where(a => a.TenantId == tenantId);
        var logs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();
        using var writer = new StreamWriter(stream);
        await writer.WriteLineAsync("Timestamp,UserId,Module,Action,Entity,Description,IpAddress");
        foreach (var log in logs)
            await writer.WriteLineAsync($"{log.Timestamp:O},{log.UserId},{log.ModuleName},{log.ActionType},{log.EntityName},{log.Description},{log.IpAddress}");
    }

    public async Task FlagSuspiciousAsync(Guid id)
    {
        var log = await _db.AuditLogs.FindAsync(id);
        if (log != null)
        {
            log.IsFlagged = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<int> DetectAndFlagSuspiciousAsync(Guid? tenantId = null)
    {
        var flagged = 0;
        var since = DateTime.UtcNow.AddHours(-24);

        var query = _db.AuditLogs.Where(a => a.Timestamp >= since && !a.IsFlagged);
        if (tenantId.HasValue) query = query.Where(a => a.TenantId == tenantId);

        var logs = await query.ToListAsync();

        var ipFailCounts = logs.Where(l => l.ActionType == "LOGIN")
            .GroupBy(l => l.IpAddress)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var log in logs)
        {
            if (ipFailCounts.TryGetValue(log.IpAddress, out var count) && count >= 5)
            {
                log.IsFlagged = true; flagged++;
                continue;
            }

            if (log.ActionType == "API_CALL" && log.Description?.Contains("DELETE") == true
                && log.Timestamp.Hour is >= 0 and <= 5)
            {
                log.IsFlagged = true; flagged++;
                continue;
            }

            if (log.ActionType == "LOGIN" && !string.IsNullOrEmpty(log.IpAddress) && !string.IsNullOrEmpty(log.DeviceInfo))
            {
                var lastLogin = _db.AuditLogs
                    .Where(a => a.UserId == log.UserId && a.ActionType == "LOGIN" && a.Timestamp < log.Timestamp)
                    .OrderByDescending(a => a.Timestamp)
                    .FirstOrDefault();
                if (lastLogin != null && lastLogin.IpAddress != log.IpAddress)
                {
                    log.IsFlagged = true; flagged++;
                }
            }
        }

        if (flagged > 0) await _db.SaveChangesAsync();
        return flagged;
    }

    public async Task<List<AuditEntryDto>> GetFlaggedEntriesAsync(Guid? tenantId, int page = 1, int pageSize = 50)
    {
        var query = _db.AuditLogs.Where(a => a.IsFlagged);
        if (tenantId.HasValue) query = query.Where(a => a.TenantId == tenantId);
        var logs = await query.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return logs.Select(a => new AuditEntryDto
        {
            Timestamp = a.Timestamp, UserId = a.UserId, TenantId = a.TenantId,
            ModuleName = a.ModuleName,
            ActionType = Enum.TryParse<ActionType>(a.ActionType, out var at) ? at : ActionType.ApiCall,
            EntityName = a.EntityName, Details = a.Description,
            IpAddress = a.IpAddress, DeviceInfo = a.DeviceInfo, UserAgent = a.UserAgent, IsSuspicious = true
        }).ToList();
    }
}
