using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class IntegrationQueueService : IIntegrationQueueService
{
    private readonly YuktiraDbContext _db;
    private readonly HttpClient _httpClient;

    public IntegrationQueueService(YuktiraDbContext db, HttpClient httpClient)
    {
        _db = db;
        _httpClient = httpClient;
    }

    public async Task EnqueueAsync(Guid tenantId, string messageType, object payload, string targetSystem = "")
    {
        _db.IntegrationQueues.Add(new IntegrationQueueEntity
        {
            TenantId = tenantId,
            MessageType = messageType,
            Payload = JsonSerializer.Serialize(payload),
            Status = "Pending",
            RetryCount = 0,
            MaxRetries = 3,
            Direction = "Outbound",
            TargetSystem = targetSystem,
            NextRetryAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<IntegrationQueueDto>> GetPendingAsync(Guid tenantId, int limit = 50)
    {
        return await _db.IntegrationQueues
            .Where(q => q.TenantId == tenantId && q.Status == "Pending" && q.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(q => q.CreatedAt)
            .Take(limit)
            .Select(q => new IntegrationQueueDto
            {
                Id = q.Id,
                MessageType = q.MessageType,
                Status = q.Status,
                RetryCount = q.RetryCount,
                LastError = q.LastError,
                NextRetryAt = q.NextRetryAt,
                TargetSystem = q.TargetSystem,
                CreatedAt = q.CreatedAt
            })
            .ToListAsync();
    }

    public async Task ProcessQueueAsync(Guid tenantId)
    {
        var pending = await _db.IntegrationQueues
            .Where(q => q.TenantId == tenantId && q.Status == "Pending" && q.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(q => q.CreatedAt)
            .Take(50)
            .ToListAsync();

        foreach (var item in pending)
        {
            try
            {
                if (!string.IsNullOrEmpty(item.TargetSystem))
                {
                    var content = new StringContent(item.Payload, System.Text.Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(item.TargetSystem, content);
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"Target returned {response.StatusCode}");
                }

                item.Status = "Processed";
                item.UpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                item.RetryCount++;
                item.LastError = ex.Message;
                item.UpdatedAt = DateTime.UtcNow;

                if (item.RetryCount >= item.MaxRetries)
                {
                    item.Status = "Failed";
                    _db.IntegrationDeadLetters.Add(new IntegrationDeadLetterEntity
                    {
                        TenantId = item.TenantId,
                        OriginalQueueId = item.Id,
                        MessageType = item.MessageType,
                        Payload = item.Payload,
                        ErrorMessage = ex.Message,
                        RetryAttempts = item.RetryCount,
                        FailedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    item.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, item.RetryCount) * 5);
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<IntegrationDeadLetterDto>> GetDeadLetterAsync(Guid tenantId)
    {
        return await _db.IntegrationDeadLetters
            .Where(d => d.TenantId == tenantId)
            .OrderByDescending(d => d.FailedAt)
            .Select(d => new IntegrationDeadLetterDto
            {
                Id = d.Id,
                MessageType = d.MessageType,
                ErrorMessage = d.ErrorMessage,
                RetryAttempts = d.RetryAttempts,
                FailedAt = d.FailedAt
            })
            .ToListAsync();
    }

    public async Task RequeueDeadLetterAsync(Guid deadLetterId)
    {
        var dead = await _db.IntegrationDeadLetters.FindAsync(deadLetterId);
        if (dead == null) return;

        _db.IntegrationQueues.Add(new IntegrationQueueEntity
        {
            TenantId = dead.TenantId,
            MessageType = dead.MessageType,
            Payload = dead.Payload,
            Status = "Pending",
            RetryCount = 0,
            MaxRetries = 3,
            Direction = "Outbound",
            TargetSystem = "",
            NextRetryAt = DateTime.UtcNow
        });

        _db.IntegrationDeadLetters.Remove(dead);
        await _db.SaveChangesAsync();
    }
}
