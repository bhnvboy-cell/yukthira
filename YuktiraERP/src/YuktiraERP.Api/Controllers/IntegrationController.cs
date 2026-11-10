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
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
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

    [HttpPost("queue/enqueue")]
    public async Task<IActionResult> EnqueueMessage([FromBody] EnqueueMessageRequest req)
    {
        var qsvc = HttpContext.RequestServices.GetRequiredService<IIntegrationQueueService>();
        await qsvc.EnqueueAsync(GetTenantId(), req.MessageType, req.Payload, req.TargetSystem ?? "");
        return Ok(new { message = "Message enqueued successfully", messageType = req.MessageType });
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

    [HttpPost("queue/requeue/{id}")]
    public async Task<IActionResult> RequeueDeadLetter(Guid id)
    {
        var qsvc = HttpContext.RequestServices.GetRequiredService<IIntegrationQueueService>();
        await qsvc.RequeueDeadLetterAsync(id);
        return Ok(new { message = "Message requeued successfully", id });
    }

    // ── Client Validation (Public/Anonymous allowed) ──

    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateApiClient([FromBody] ValidateApiClientRequest req)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var isValid = await _hub.ValidateApiClientAsync(req.ClientId, req.ClientSecret, ip);
        return Ok(new { valid = isValid, clientId = req.ClientId });
    }

    // ── Event Dispatching ──

    [HttpPost("dispatch")]
    public async Task<IActionResult> DispatchEvent([FromBody] IntegrationDispatchRequest req)
    {
        await _hub.DispatchWebhookEventAsync(new WebhookEvent
        {
            TenantId = GetTenantId(),
            EventType = req.EventType,
            EntityType = req.EntityType,
            EntityId = req.EntityId,
            Payload = req.Payload
        });
        return Ok(new { message = "Webhook event dispatched successfully", eventType = req.EventType, entityId = req.EntityId });
    }

    // ── EDI conversion ──

    [HttpPost("edi/convert")]
    public async Task<IActionResult> ConvertEdi([FromBody] EdiUnifiedConvertRequest req)
    {
        try
        {
            var svc = HttpContext.RequestServices.GetRequiredService<IEdiService>();
            var isTargetJson = string.Equals(req.TargetFormat, "JSON", StringComparison.OrdinalIgnoreCase);
            if (isTargetJson)
            {
                var isX12 = string.Equals(req.SourceFormat, "X12", StringComparison.OrdinalIgnoreCase);
                var parsed = isX12
                    ? await svc.ParseX12Async(req.EdiContent ?? "")
                    : await svc.ParseEdifactAsync(req.EdiContent ?? "");
                return Ok(new { sourceFormat = req.SourceFormat, targetFormat = req.TargetFormat, result = parsed });
            }
            else
            {
                var isX12 = string.Equals(req.TargetFormat, "X12", StringComparison.OrdinalIgnoreCase);
                var docType = string.IsNullOrWhiteSpace(req.DocumentType) ? "ORDERS" : req.DocumentType;
                var output = isX12
                    ? await svc.ConvertToX12Async(req.EdiContent ?? "{}", docType)
                    : await svc.ConvertToEdifactAsync(req.EdiContent ?? "{}", docType);
                return Ok(new { sourceFormat = req.SourceFormat, targetFormat = req.TargetFormat, content = output });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("edi/convert/edifact")]
    public async Task<IActionResult> ConvertToEdifact([FromBody] EdiConversionRequest req)
    {
        try
        {
            var svc = HttpContext.RequestServices.GetRequiredService<IEdiService>();
            var result = await svc.ConvertToEdifactAsync(req.Data, req.DocumentType);
            return Ok(new { format = "EDIFACT", documentType = req.DocumentType, content = result });
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("edi/convert/x12")]
    public async Task<IActionResult> ConvertToX12([FromBody] EdiConversionRequest req)
    {
        try
        {
            var svc = HttpContext.RequestServices.GetRequiredService<IEdiService>();
            var result = await svc.ConvertToX12Async(req.Data, req.DocumentType);
            return Ok(new { format = "X12", documentType = req.DocumentType, content = result });
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    private Guid GetTenantId()
    {
        if (Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) && tid != Guid.Empty)
            return tid;
        if (HttpContext.Items.TryGetValue("TenantId", out var itemVal) && itemVal is Guid gid && gid != Guid.Empty)
            return gid;
        if (itemVal is string strVal && Guid.TryParse(strVal, out var parsedGid) && parsedGid != Guid.Empty)
            return parsedGid;
        var fallback = _db.Tenants.Select(t => t.Id).FirstOrDefault();
        return fallback != Guid.Empty ? fallback : Guid.Empty;
    }
}

public record CreateWebhookRequest(string Name, string EventType, string TargetUrl, string? SecretKey = null);
public record CreateApiClientRequest(string Name, string[]? AllowedIpAddresses);
public record EdiConversionRequest(string DocumentType, object Data);
public record EdiParseRequest(string Content);
public record EnqueueMessageRequest(string MessageType, object Payload, string? TargetSystem = null);
public record ValidateApiClientRequest(string ClientId, string ClientSecret);
public record IntegrationDispatchRequest(string EventType, string EntityType, string EntityId, Dictionary<string, object>? Payload = null);
public record EdiUnifiedConvertRequest(string EdiContent, string? SourceFormat = "EDIFACT", string? TargetFormat = "JSON", string? DocumentType = "ORDERS");
