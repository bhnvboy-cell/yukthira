using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class AuthorizationTraceService : IAuthorizationTraceService
{
    private readonly YuktiraDbContext _db;
    private static int _writeCounter;

    public AuthorizationTraceService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task TraceAsync(AuthorizationTraceEntry entry)
    {
        try
        {
            _db.AuthorizationTraces.Add(new AuthorizationTraceEntity
            {
                Id = Guid.NewGuid(),
                TenantId = entry.TenantId,
                UserId = entry.UserId,
                UserName = entry.UserName,
                Role = entry.Role,
                SessionId = entry.SessionId,
                CorrelationId = entry.CorrelationId,
                ResourceType = entry.ResourceType,
                Resource = entry.Resource,
                Decision = entry.Decision,
                RuleSource = entry.RuleSource,
                Reason = entry.Reason,
                HttpMethod = entry.HttpMethod,
                HttpPath = entry.HttpPath,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent
            });
            await _db.SaveChangesAsync();
            _writeCounter++;
            if (_writeCounter % 50 == 0)
            {
                var cutoff = DateTime.UtcNow.AddDays(-7);
                var stale = await _db.AuthorizationTraces.Where(t => t.CreatedAt < cutoff).ToListAsync();
                if (stale.Count > 0)
                {
                    _db.AuthorizationTraces.RemoveRange(stale);
                    await _db.SaveChangesAsync();
                }
            }
        }
        catch
        {
        }
    }

    public async Task<AuthorizationTraceResult> QueryAsync(AuthorizationTraceQuery query, Guid? tenantId)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, Math.Min(query.PageSize, 500));
        var filtered = BuildQuery(tenantId, query);
        var totalCount = await filtered.CountAsync();
        var rows = await filtered
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AuthorizationTraceResult
        {
            Entries = rows.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task ExportToCsvAsync(Guid? tenantId, Stream stream, AuthorizationTraceQuery? filter = null)
    {
        var rows = await BuildQuery(tenantId, filter)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteLineAsync("Timestamp,User,Role,Decision,ResourceType,Resource,RuleSource,Reason,HttpMethod,HttpPath,IpAddress,CorrelationId");
        foreach (var t in rows)
        {
            await writer.WriteLineAsync(string.Join(",",
                EscapeCsv(t.CreatedAt.ToString("O")),
                EscapeCsv(t.UserName),
                EscapeCsv(t.Role),
                EscapeCsv(t.Decision),
                EscapeCsv(t.ResourceType),
                EscapeCsv(t.Resource),
                EscapeCsv(t.RuleSource),
                EscapeCsv(t.Reason),
                EscapeCsv(t.HttpMethod),
                EscapeCsv(t.HttpPath),
                EscapeCsv(t.IpAddress),
                EscapeCsv(t.CorrelationId)));
        }
    }

    public async Task<int> PurgeAsync(Guid? tenantId, DateTime? olderThan = null)
    {
        var cutoff = olderThan ?? DateTime.UtcNow.AddDays(-7);
        var query = _db.AuthorizationTraces.AsQueryable();
        if (tenantId.HasValue) query = query.Where(t => t.TenantId == tenantId.Value);
        var stale = await query.Where(t => t.CreatedAt < cutoff).ToListAsync();
        if (stale.Count == 0) return 0;
        _db.AuthorizationTraces.RemoveRange(stale);
        await _db.SaveChangesAsync();
        return stale.Count;
    }

    private IQueryable<AuthorizationTraceEntity> BuildQuery(Guid? tenantId, AuthorizationTraceQuery? filter)
    {
        var query = _db.AuthorizationTraces.AsQueryable();
        if (tenantId.HasValue) query = query.Where(t => t.TenantId == tenantId.Value);
        if (filter == null) return query;

        if (filter.UserId.HasValue) query = query.Where(t => t.UserId == filter.UserId.Value);
        if (!string.IsNullOrEmpty(filter.UserName))
        {
            var term = filter.UserName.ToLower();
            query = query.Where(t => t.UserName.ToLower().Contains(term));
        }
        if (!string.IsNullOrEmpty(filter.Decision)) query = query.Where(t => t.Decision == filter.Decision);
        if (!string.IsNullOrEmpty(filter.ResourceType)) query = query.Where(t => t.ResourceType == filter.ResourceType);
        if (!string.IsNullOrEmpty(filter.Resource))
        {
            var term = filter.Resource.ToLower();
            query = query.Where(t => t.Resource.ToLower().Contains(term));
        }
        if (!string.IsNullOrEmpty(filter.RuleSource)) query = query.Where(t => t.RuleSource == filter.RuleSource);
        if (filter.From.HasValue) query = query.Where(t => t.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(t => t.CreatedAt <= filter.To.Value);
        return query;
    }

    private static AuthorizationTraceDto ToDto(AuthorizationTraceEntity t) => new AuthorizationTraceDto
    {
        Id = t.Id,
        CreatedAt = t.CreatedAt,
        UserId = t.UserId,
        UserName = t.UserName,
        Role = t.Role,
        ResourceType = t.ResourceType,
        Resource = t.Resource,
        Decision = t.Decision,
        RuleSource = t.RuleSource,
        Reason = t.Reason,
        CorrelationId = t.CorrelationId,
        HttpMethod = t.HttpMethod,
        HttpPath = t.HttpPath,
        IpAddress = t.IpAddress
    };

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
