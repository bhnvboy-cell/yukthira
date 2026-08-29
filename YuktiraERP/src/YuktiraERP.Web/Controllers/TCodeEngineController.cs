using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class TCodeEngineController : ControllerBase
{
    private readonly ITCodeLayoutRegistry _registry;
    private readonly YuktiraDbContext _db;

    public TCodeEngineController(ITCodeLayoutRegistry registry, YuktiraDbContext db)
    {
        _registry = registry;
        _db = db;
    }

    [HttpGet("layout/{tcode}")]
    public IActionResult GetLayout(string tcode)
    {
        var config = _registry.Get(tcode);
        if (config is null) return NotFound(new { error = $"No layout config for '{tcode}'" });
        return Ok(config);
    }

    [HttpGet("layouts")]
    public IActionResult GetAllLayouts()
    {
        return Ok(_registry.GetAll().Select(c => new { c.TCode, c.Title, c.Module, c.Icon }));
    }

    [HttpGet("layout/{tcode}/records")]
    public async Task<IActionResult> GetRecords(string tcode)
    {
        var config = _registry.Get(tcode);
        if (config is null) return NotFound();

        var tenantId = GetTenantId();
        var tcodeEntity = await _db.TransactionCodes.FirstOrDefaultAsync(x => x.Code == tcode);
        if (tcodeEntity is null) return Ok(new List<object>());

        var records = await _db.TCodeData
            .Where(x => x.TenantId == tenantId && x.TCodeId == tcodeEntity.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var rows = records.Select(r =>
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(r.DataJson) ?? new();
            dict["__id"] = r.RecordId;
            dict["__recordId"] = r.RecordId;
            return dict;
        }).ToList();

        return Ok(rows);
    }

    [HttpPost("layout/{tcode}/records")]
    public async Task<IActionResult> SaveRecords(string tcode, [FromBody] SaveRecordsRequest request)
    {
        var config = _registry.Get(tcode);
        if (config is null) return NotFound();

        var tenantId = GetTenantId();
        var userId = GetUserId();
        var tcodeEntity = await _db.TransactionCodes.FirstOrDefaultAsync(x => x.Code == tcode);
        if (tcodeEntity is null) return BadRequest(new { error = "Transaction code not found in DB" });

        var savedCount = 0;
        foreach (var row in request.Records ?? new())
        {
            var recordId = row.TryGetValue("__recordId", out var rid) ? rid?.ToString() : null;
            var json = JsonSerializer.Serialize(row.Where(k => !k.Key.StartsWith("__")).ToDictionary(k => k.Key, k => k.Value));

            if (!string.IsNullOrEmpty(recordId))
            {
                var existing = await _db.TCodeData.FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId && x.TCodeId == tcodeEntity.Id && x.RecordId == recordId);
                if (existing != null)
                {
                    existing.DataJson = json;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = userId;
                    savedCount++;
                    continue;
                }
            }

            var newRecord = new TCodeDataEntity
            {
                TenantId = tenantId,
                TCodeId = tcodeEntity.Id,
                RecordId = Guid.NewGuid().ToString("N")[..12],
                DataJson = json,
                Status = "ACTIVE",
                WorkflowNode = "",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            _db.TCodeData.Add(newRecord);
            savedCount++;
        }

        foreach (var delId in request.DeleteIds ?? new())
        {
            var del = await _db.TCodeData.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.TCodeId == tcodeEntity.Id && x.RecordId == delId);
            if (del != null) _db.TCodeData.Remove(del);
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, saved = savedCount, deleted = (request.DeleteIds ?? new()).Count });
    }

    [HttpPost("layout/{tcode}/action")]
    public async Task<IActionResult> ExecuteAction(string tcode, [FromBody] TCodeActionRequest request)
    {
        var config = _registry.Get(tcode);
        if (config is null) return NotFound();

        return request.Action switch
        {
            "save" => await SaveRecords(tcode, new SaveRecordsRequest { Records = request.Payload?.GetValueOrDefault("records") as List<Dictionary<string, object>> }),
            "refresh" => await GetRecords(tcode),
            _ => Ok(new { success = true, action = request.Action, message = $"Action '{request.Action}' executed for {tcode}" })
        };
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return string.IsNullOrEmpty(claim) ? Guid.Parse("54e6957e-4db2-4731-a7f4-2e50e597bf91") : Guid.Parse(claim);
    }

    private Guid GetUserId()
    {
        var claim = System.Security.Claims.ClaimTypes.NameIdentifier;
        var val = User.FindFirst(claim)?.Value;
        return string.IsNullOrEmpty(val) ? Guid.Empty : Guid.Parse(val);
    }
}

public class TCodeActionRequest
{
    public string Action { get; set; } = "";
    public Dictionary<string, object>? Payload { get; set; }
}

public class SaveRecordsRequest
{
    public List<Dictionary<string, object>>? Records { get; set; }
    public List<string>? DeleteIds { get; set; }
}
