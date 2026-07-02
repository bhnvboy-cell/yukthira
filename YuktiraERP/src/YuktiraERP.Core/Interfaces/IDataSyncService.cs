namespace YuktiraERP.Core.Interfaces;

public interface IDataSyncService
{
    Task<SyncJobDto> CreateJobAsync(Guid tenantId, SyncJobDto job);
    Task<List<SyncJobDto>> GetJobsAsync(Guid tenantId);
    Task<SyncJobDto?> GetJobAsync(Guid tenantId, Guid jobId);
    Task<bool> DeleteJobAsync(Guid tenantId, Guid jobId);
    Task<SyncJobDto> RunJobAsync(Guid tenantId, Guid jobId);
    Task<List<SyncLogDto>> GetJobLogsAsync(Guid tenantId, Guid jobId, int page = 1, int pageSize = 20);
}

public class SyncJobDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ConnectorType { get; set; } = "";
    public Guid ConnectionId { get; set; }
    public string ConnectionName { get; set; } = "";
    public string Direction { get; set; } = "Pull";
    public string EntityType { get; set; } = "";
    public string ScheduleType { get; set; } = "Manual";
    public string CronExpression { get; set; } = "";
    public string ConflictResolution { get; set; } = "SourceWins";
    public bool IsActive { get; set; }
    public DateTime? LastRunAt { get; set; }
    public string LastRunResult { get; set; } = "";
}

public class SyncLogDto
{
    public Guid Id { get; set; }
    public Guid SyncJobId { get; set; }
    public string Direction { get; set; } = "";
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int ConflictCount { get; set; }
    public string ErrorDetail { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
