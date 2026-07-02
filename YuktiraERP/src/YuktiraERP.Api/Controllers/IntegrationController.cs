using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/integration")]
[Authorize(Roles = "SUPER_USER,ADMIN")]
public class IntegrationController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly ConnectorRegistry _registry;
    private readonly IIntegrationHub _hub;
    private readonly IDataSyncService _sync;

    public IntegrationController(YuktiraDbContext db, ConnectorRegistry registry, IIntegrationHub hub, IDataSyncService sync)
    {
        _db = db; _registry = registry; _hub = hub; _sync = sync;
    }

    // ── Connectors ──

    [HttpGet("connectors")]
    public IActionResult GetConnectors()
    {
        var connectors = _registry.GetAll().Select(c => new ConnectorDto
        {
            Type = c.ConnectorType, Name = c.Name, Version = c.Version,
            Description = c.Description, SupportedAuthTypes = c.SupportedAuthTypes,
            SupportedActions = c.SupportedActions
        }).ToList();
        return Ok(connectors);
    }

    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections()
    {
        var tenantId = GetTenantId();
        var list = await _db.IntegrationConnections
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new IntegrationConnectionDto
            {
                Id = c.Id, ConnectorType = c.ConnectorType, Name = c.Name,
                BaseUrl = c.BaseUrl, AuthType = c.AuthType, IsActive = c.IsActive,
                TimeoutSeconds = c.TimeoutSeconds, LastTestedAt = c.LastTestedAt,
                LastTestResult = c.LastTestResult
            }).ToListAsync();
        return Ok(list);
    }

    [HttpPost("connections")]
    public async Task<IActionResult> CreateConnection([FromBody] IntegrationConnectionDto dto)
    {
        var conn = new IntegrationConnectionEntity
        {
            TenantId = GetTenantId(), ConnectorType = dto.ConnectorType, Name = dto.Name,
            BaseUrl = dto.BaseUrl, AuthType = dto.AuthType, IsActive = dto.IsActive,
            TimeoutSeconds = dto.TimeoutSeconds
        };
        _db.IntegrationConnections.Add(conn);
        await _db.SaveChangesAsync();
        dto.Id = conn.Id;
        return Ok(dto);
    }

    [HttpPut("connections/{id}")]
    public async Task<IActionResult> UpdateConnection(Guid id, [FromBody] IntegrationConnectionDto dto)
    {
        var conn = await _db.IntegrationConnections.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == GetTenantId());
        if (conn == null) return NotFound();
        conn.Name = dto.Name; conn.BaseUrl = dto.BaseUrl; conn.AuthType = dto.AuthType;
        conn.IsActive = dto.IsActive; conn.TimeoutSeconds = dto.TimeoutSeconds;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("connections/{id}")]
    public async Task<IActionResult> DeleteConnection(Guid id)
    {
        var conn = await _db.IntegrationConnections.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == GetTenantId());
        if (conn == null) return NotFound();
        _db.IntegrationConnections.Remove(conn);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("connections/{id}/test")]
    public async Task<IActionResult> TestConnection(Guid id, [FromBody] Dictionary<string, string>? authConfig)
    {
        var conn = await _db.IntegrationConnections.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == GetTenantId());
        if (conn == null) return NotFound();
        var connector = _registry.Get(conn.ConnectorType);
        if (connector == null) return BadRequest("Connector not found");
        var ac = JsonSerializer.Serialize(authConfig ?? new());
        var result = await connector.TestConnectionAsync(conn.BaseUrl, conn.AuthType,
            authConfig ?? new(), new());
        conn.LastTestedAt = DateTime.UtcNow;
        conn.LastTestResult = result.Success ? "OK" : result.Message;
        conn.AuthConfigJson = ac;
        await _db.SaveChangesAsync();
        return Ok(result);
    }

    [HttpPost("connections/{id}/execute")]
    public async Task<IActionResult> ExecuteAction(Guid id, [FromBody] ConnectorActionRequest req)
    {
        var conn = await _db.IntegrationConnections.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == GetTenantId());
        if (conn == null) return NotFound();
        var connector = _registry.Get(conn.ConnectorType);
        if (connector == null) return BadRequest("Connector not found");
        var authConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(conn.AuthConfigJson) ?? new();
        var addConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(conn.AdditionalConfigJson) ?? new();
        var result = await connector.ExecuteActionAsync(conn.BaseUrl, conn.AuthType, authConfig, addConfig, req.Action, req.Parameters);
        return Ok(result);
    }

    // ── Webhooks ──

    [HttpGet("webhooks")]
    public async Task<IActionResult> GetWebhooks() => Ok(await _hub.GetWebhooksAsync(GetTenantId()));

    [HttpPost("webhooks")]
    public async Task<IActionResult> CreateWebhook([FromBody] CreateWebhookRequest req)
    {
        await _hub.RegisterWebhookAsync(GetTenantId(), req.Name, req.EventType, req.TargetUrl, req.SecretKey);
        return Ok();
    }

    [HttpDelete("webhooks/{id}")]
    public async Task<IActionResult> DeleteWebhook(Guid id)
    {
        var ok = await _hub.DeleteWebhookAsync(id);
        return ok ? Ok() : NotFound();
    }

    [HttpGet("webhooks/event-types")]
    public async Task<IActionResult> GetEventTypes()
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IWebhookService>();
        return Ok(await svc.GetSupportedEventTypesAsync());
    }

    [HttpGet("webhooks/{id}/logs")]
    public async Task<IActionResult> GetWebhookLogs(Guid id, [FromQuery] int page = 1)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IWebhookService>();
        return Ok(await svc.GetDeliveryLogsAsync(GetTenantId(), id, page));
    }

    // ── Mapping Rules ──

    [HttpGet("mappings")]
    public async Task<IActionResult> GetMappings()
    {
        var list = await _db.MappingRules.Where(m => m.TenantId == GetTenantId())
            .OrderByDescending(m => m.CreatedAt).ToListAsync();
        return Ok(list.Select(m => new MappingRuleDto
        {
            Id = m.Id, Name = m.Name, SourceSystem = m.SourceSystem, TargetSystem = m.TargetSystem,
            SourceEntity = m.SourceEntity, TargetEntity = m.TargetEntity,
            FieldMappings = JsonSerializer.Deserialize<List<FieldMappingDto>>(m.FieldMappingsJson) ?? new(),
            TransformationScript = m.TransformationScript, IsActive = m.IsActive
        }));
    }

    [HttpPost("mappings")]
    public async Task<IActionResult> SaveMapping([FromBody] MappingRuleDto dto)
    {
        var entity = new MappingRuleEntity
        {
            TenantId = GetTenantId(), Name = dto.Name, SourceSystem = dto.SourceSystem,
            TargetSystem = dto.TargetSystem, SourceEntity = dto.SourceEntity, TargetEntity = dto.TargetEntity,
            FieldMappingsJson = JsonSerializer.Serialize(dto.FieldMappings),
            TransformationScript = dto.TransformationScript, IsActive = dto.IsActive
        };
        _db.MappingRules.Add(entity);
        await _db.SaveChangesAsync();
        dto.Id = entity.Id;
        return Ok(dto);
    }

    [HttpDelete("mappings/{id}")]
    public async Task<IActionResult> DeleteMapping(Guid id)
    {
        var m = await _db.MappingRules.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == GetTenantId());
        if (m == null) return NotFound();
        _db.MappingRules.Remove(m);
        await _db.SaveChangesAsync();
        return Ok();
    }

    // ── API Clients ──

    [HttpGet("api-clients")]
    public async Task<IActionResult> GetApiClients()
    {
        var list = await _db.ApiClients.Where(c => c.TenantId == GetTenantId()).ToListAsync();
        return Ok(list.Select(c => new { c.Id, c.ClientId, c.Name, c.AllowedIpAddresses, c.IsActive }));
    }

    [HttpPost("api-clients")]
    public async Task<IActionResult> CreateApiClient([FromBody] CreateApiClientRequest req)
    {
        var clientId = Guid.NewGuid().ToString("N")[..16];
        var secret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        _db.ApiClients.Add(new ApiClientEntity
        {
            TenantId = GetTenantId(), ClientId = clientId, ClientSecret = secret,
            Name = req.Name, AllowedIpAddresses = req.AllowedIpAddresses ?? Array.Empty<string>()
        });
        await _db.SaveChangesAsync();
        return Ok(new { clientId, secret });
    }

    [HttpDelete("api-clients/{id}")]
    public async Task<IActionResult> DeleteApiClient(Guid id)
    {
        var c = await _db.ApiClients.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == GetTenantId());
        if (c == null) return NotFound();
        _db.ApiClients.Remove(c);
        await _db.SaveChangesAsync();
        return Ok();
    }

    // ── Sync Jobs ──

    [HttpGet("sync-jobs")]
    public async Task<IActionResult> GetSyncJobs() => Ok(await _sync.GetJobsAsync(GetTenantId()));

    [HttpPost("sync-jobs")]
    public async Task<IActionResult> CreateSyncJob([FromBody] SyncJobDto dto) => Ok(await _sync.CreateJobAsync(GetTenantId(), dto));

    [HttpPost("sync-jobs/{id}/run")]
    public async Task<IActionResult> RunSyncJob(Guid id)
    {
        try
        {
            var result = await _sync.RunJobAsync(GetTenantId(), id);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("sync-jobs/{id}")]
    public async Task<IActionResult> DeleteSyncJob(Guid id)
    {
        var ok = await _sync.DeleteJobAsync(GetTenantId(), id);
        return ok ? Ok() : NotFound();
    }

    [HttpGet("sync-jobs/{id}/logs")]
    public async Task<IActionResult> GetSyncLogs(Guid id, [FromQuery] int page = 1) => Ok(await _sync.GetJobLogsAsync(GetTenantId(), id, page));

    // ── Queue ──

    [HttpGet("queue/pending")]
    public async Task<IActionResult> GetPendingQueue()
    {
        var qsvc = HttpContext.RequestServices.GetRequiredService<IIntegrationQueueService>();
        return Ok(await qsvc.GetPendingAsync(GetTenantId()));
    }

    [HttpPost("queue/process")]
    public async Task<IActionResult> ProcessQueue()
    {
        var qsvc = HttpContext.RequestServices.GetRequiredService<IIntegrationQueueService>();
        await qsvc.ProcessQueueAsync(GetTenantId());
        return Ok();
    }

    [HttpGet("queue/dead-letter")]
    public async Task<IActionResult> GetDeadLetter()
    {
        var qsvc = HttpContext.RequestServices.GetRequiredService<IIntegrationQueueService>();
        return Ok(await qsvc.GetDeadLetterAsync(GetTenantId()));
    }

    private Guid GetTenantId() =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;
}

public record CreateWebhookRequest(string Name, string EventType, string TargetUrl, string? SecretKey);
public record CreateApiClientRequest(string Name, string[]? AllowedIpAddresses);
