using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class DataSyncService : IDataSyncService
{
    private readonly YuktiraDbContext _db;
    private readonly ConnectorRegistry _registry;

    public DataSyncService(YuktiraDbContext db, ConnectorRegistry registry)
    {
        _db = db;
        _registry = registry;
    }

    public async Task<SyncJobDto> CreateJobAsync(Guid tenantId, SyncJobDto job)
    {
        var entity = new SyncJobEntity
        {
            TenantId = tenantId, Name = job.Name, ConnectorType = job.ConnectorType,
            ConnectionId = job.ConnectionId, Direction = job.Direction, EntityType = job.EntityType,
            ScheduleType = job.ScheduleType, CronExpression = job.CronExpression,
            ConflictResolution = job.ConflictResolution, IsActive = job.IsActive
        };
        _db.Add(entity);
        await _db.SaveChangesAsync();
        job.Id = entity.Id;
        return job;
    }

    public async Task<List<SyncJobDto>> GetJobsAsync(Guid tenantId)
    {
        return await _db.SyncJobs.Where(j => j.TenantId == tenantId)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new SyncJobDto
            {
                Id = j.Id, Name = j.Name, ConnectorType = j.ConnectorType,
                ConnectionId = j.ConnectionId, Direction = j.Direction, EntityType = j.EntityType,
                ScheduleType = j.ScheduleType, CronExpression = j.CronExpression,
                ConflictResolution = j.ConflictResolution, IsActive = j.IsActive,
                LastRunAt = j.LastRunAt, LastRunResult = j.LastRunResult
            }).ToListAsync();
    }

    public async Task<SyncJobDto?> GetJobAsync(Guid tenantId, Guid jobId)
    {
        var j = await _db.SyncJobs.FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == tenantId);
        if (j == null) return null;
        var conn = await _db.IntegrationConnections.FindAsync(j.ConnectionId);
        return new SyncJobDto
        {
            Id = j.Id, Name = j.Name, ConnectorType = j.ConnectorType,
            ConnectionId = j.ConnectionId, ConnectionName = conn?.Name ?? "",
            Direction = j.Direction, EntityType = j.EntityType,
            ScheduleType = j.ScheduleType, CronExpression = j.CronExpression,
            ConflictResolution = j.ConflictResolution, IsActive = j.IsActive,
            LastRunAt = j.LastRunAt, LastRunResult = j.LastRunResult
        };
    }

    public async Task<bool> DeleteJobAsync(Guid tenantId, Guid jobId)
    {
        var j = await _db.SyncJobs.FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == tenantId);
        if (j == null) return false;
        _db.SyncJobs.Remove(j);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<SyncJobDto> RunJobAsync(Guid tenantId, Guid jobId)
    {
        var job = await _db.SyncJobs.FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == tenantId)
            ?? throw new InvalidOperationException("Job not found");
        var conn = await _db.IntegrationConnections.FindAsync(job.ConnectionId)
            ?? throw new InvalidOperationException("Connection not found");

        var connector = _registry.Get(job.ConnectorType);
        if (connector == null) throw new InvalidOperationException($"No connector registered for {job.ConnectorType}");

        var authConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(conn.AuthConfigJson) ?? new();
        var addConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(conn.AdditionalConfigJson) ?? new();

        var log = new SyncLogEntity
        {
            TenantId = tenantId, SyncJobId = jobId, Direction = job.Direction,
            StartedAt = DateTime.UtcNow
        };
        _db.SyncLogs.Add(log);

        try
        {
            if (job.Direction == "Pull")
            {
                var data = await connector.PullDataAsync(conn.BaseUrl, conn.AuthType, authConfig, addConfig, job.EntityType, job.LastRunAt);
                log.TotalRecords = data.Count;
                log.SuccessCount = data.Count;
            }
            else
            {
                var success = await connector.PushDataAsync(conn.BaseUrl, conn.AuthType, authConfig, addConfig, job.EntityType, new());
                log.TotalRecords = 0;
                log.SuccessCount = success ? 0 : 0;
            }

            job.LastRunAt = DateTime.UtcNow;
            job.LastRunResult = "Success";
            log.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            job.LastRunResult = $"Failed: {ex.Message}";
            log.ErrorCount = 1;
            log.ErrorDetail = ex.Message;
            log.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (await GetJobAsync(tenantId, jobId))!;
    }

    public async Task<List<SyncLogDto>> GetJobLogsAsync(Guid tenantId, Guid jobId, int page = 1, int pageSize = 20)
    {
        return await _db.SyncLogs
            .Where(l => l.TenantId == tenantId && l.SyncJobId == jobId)
            .OrderByDescending(l => l.StartedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new SyncLogDto
            {
                Id = l.Id, SyncJobId = l.SyncJobId, Direction = l.Direction,
                TotalRecords = l.TotalRecords, SuccessCount = l.SuccessCount,
                ErrorCount = l.ErrorCount, ConflictCount = l.ConflictCount,
                ErrorDetail = l.ErrorDetail, StartedAt = l.StartedAt, CompletedAt = l.CompletedAt
            }).ToListAsync();
    }
}
