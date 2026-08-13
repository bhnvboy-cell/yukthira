using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/integration/edi")]
[Authorize(Roles = "SUPER_USER,ADMIN")]
public class EdiController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly IEdiService _edi;

    public EdiController(YuktiraDbContext db, IEdiService edi)
    {
        _db = db;
        _edi = edi;
    }

    private Guid TenantId =>
        Guid.TryParse(User.FindFirst("TenantId")?.Value, out var tid) ? tid : Guid.Empty;

    // ── Trading Partners ──

    [HttpGet("partners")]
    public async Task<IActionResult> GetPartners()
    {
        var list = await _db.EdiTradingPartners
            .Where(p => p.TenantId == TenantId)
            .OrderBy(p => p.PartnerCode)
            .ToListAsync();
        return Ok(new { data = list, tenantId = TenantId });
    }

    [HttpGet("partners/{id}")]
    public async Task<IActionResult> GetPartner(Guid id)
    {
        var partner = await _db.EdiTradingPartners
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        return partner == null ? NotFound() : Ok(partner);
    }

    [HttpPost("partners")]
    public async Task<IActionResult> CreatePartner([FromBody] EdiTradingPartnerRequest req)
    {
        var exists = await _db.EdiTradingPartners.AnyAsync(p =>
            p.TenantId == TenantId && p.PartnerCode == req.PartnerCode);
        if (exists)
            return Conflict(new { message = $"Partner {req.PartnerCode} already exists" });

        var entity = new EdiTradingPartnerEntity
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            PartnerCode = req.PartnerCode,
            PartnerName = req.PartnerName,
            Standard = string.IsNullOrWhiteSpace(req.Standard) ? "EDIFACT" : req.Standard,
            Version = string.IsNullOrWhiteSpace(req.Version) ? "D96A" : req.Version,
            SenderId = req.SenderId,
            ReceiverId = req.ReceiverId,
            SenderQualifier = string.IsNullOrWhiteSpace(req.SenderQualifier) ? "ZZ" : req.SenderQualifier,
            ReceiverQualifier = string.IsNullOrWhiteSpace(req.ReceiverQualifier) ? "ZZ" : req.ReceiverQualifier,
            TestIndicator = string.IsNullOrWhiteSpace(req.TestIndicator) ? "T" : req.TestIndicator,
            EndpointUrl = req.EndpointUrl,
            AuthType = req.AuthType,
            AuthConfigJson = req.AuthConfigJson,
            DocumentTypes = string.IsNullOrWhiteSpace(req.DocumentTypes) ? "PO,INVOICE,GRN" : req.DocumentTypes,
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.EdiTradingPartners.Add(entity);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPartner), new { id = entity.Id }, entity);
    }

    [HttpPut("partners/{id}")]
    public async Task<IActionResult> UpdatePartner(Guid id, [FromBody] EdiTradingPartnerRequest req)
    {
        var entity = await _db.EdiTradingPartners
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (entity == null) return NotFound();

        entity.PartnerName = req.PartnerName;
        entity.Standard = req.Standard;
        entity.Version = req.Version;
        entity.SenderId = req.SenderId;
        entity.ReceiverId = req.ReceiverId;
        entity.SenderQualifier = req.SenderQualifier;
        entity.ReceiverQualifier = req.ReceiverQualifier;
        entity.TestIndicator = req.TestIndicator;
        entity.EndpointUrl = req.EndpointUrl;
        entity.AuthType = req.AuthType;
        entity.AuthConfigJson = req.AuthConfigJson;
        entity.DocumentTypes = req.DocumentTypes;
        entity.IsActive = req.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(entity);
    }

    [HttpDelete("partners/{id}")]
    public async Task<IActionResult> DeletePartner(Guid id)
    {
        var entity = await _db.EdiTradingPartners
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId);
        if (entity == null) return NotFound();

        var hasLogs = await _db.EdiAcknowledgmentLogs.AnyAsync(a => a.PartnerId == id);
        if (hasLogs)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.EdiTradingPartners.Remove(entity);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = hasLogs ? "Partner deactivated (has acknowledgment history)" : "Partner deleted" });
    }

    // ── Conversion ──

    [HttpPost("convert/{standard}/{documentType}")]
    public async Task<IActionResult> Convert(string standard, string documentType, [FromBody] object? data)
    {
        var payload = data ?? new { };
        try
        {
            var result = standard.ToUpperInvariant() == "X12"
                ? await _edi.ConvertToX12Async(payload, documentType)
                : await _edi.ConvertToEdifactAsync(payload, documentType);
            return Ok(new { standard, documentType, content = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("parse/{standard}")]
    public async Task<IActionResult> Parse(string standard, [FromBody] ParseRequest req)
    {
        try
        {
            var result = standard.ToUpperInvariant() == "X12"
                ? await _edi.ParseX12Async(req.Content)
                : await _edi.ParseEdifactAsync(req.Content);
            return Ok(new { standard, parsed = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Acknowledgments ──

    [HttpPost("acknowledge")]
    public async Task<IActionResult> RecordAcknowledgment([FromBody] EdiAcknowledgmentRequest req)
    {
        var partner = await _db.EdiTradingPartners
            .FirstOrDefaultAsync(p => p.PartnerCode == req.PartnerCode && p.TenantId == TenantId);
        if (partner == null)
            return NotFound(new { message = $"Partner {req.PartnerCode} not found" });

        var entity = new EdiAcknowledgmentEntity
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            PartnerId = partner.Id,
            PartnerCode = req.PartnerCode,
            Direction = req.Direction,
            InterchangeId = req.InterchangeId,
            MessageRef = req.MessageRef,
            DocumentType = req.DocumentType,
            AckCode = req.AckCode,
            Description = req.Description,
            RawAck = req.RawAck,
            ReceivedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.EdiAcknowledgmentLogs.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Acknowledgment recorded", id = entity.Id });
    }

    [HttpGet("acknowledgments")]
    public async Task<IActionResult> GetAcknowledgmentLogs([FromQuery] string? partnerCode, [FromQuery] string? ackCode, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.EdiAcknowledgmentLogs
            .Where(a => a.TenantId == TenantId);
        if (!string.IsNullOrWhiteSpace(partnerCode))
            query = query.Where(a => a.PartnerCode == partnerCode);
        if (!string.IsNullOrWhiteSpace(ackCode))
            query = query.Where(a => a.AckCode == ackCode);

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(a => a.ReceivedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return Ok(new { data, total, page, pageSize, tenantId = TenantId });
    }

    public class EdiTradingPartnerRequest
    {
        public string PartnerCode { get; set; } = "";
        public string PartnerName { get; set; } = "";
        public string Standard { get; set; } = "EDIFACT";
        public string Version { get; set; } = "D96A";
        public string SenderId { get; set; } = "";
        public string ReceiverId { get; set; } = "";
        public string SenderQualifier { get; set; } = "ZZ";
        public string ReceiverQualifier { get; set; } = "ZZ";
        public string TestIndicator { get; set; } = "T";
        public string EndpointUrl { get; set; } = "";
        public string AuthType { get; set; } = "None";
        public string AuthConfigJson { get; set; } = "{}";
        public string DocumentTypes { get; set; } = "PO,INVOICE,GRN";
        public bool IsActive { get; set; } = true;
    }

    public class ParseRequest { public string Content { get; set; } = ""; }

    public class EdiAcknowledgmentRequest
    {
        public string PartnerCode { get; set; } = "";
        public string Direction { get; set; } = "Outbound";
        public string InterchangeId { get; set; } = "";
        public string MessageRef { get; set; } = "";
        public string DocumentType { get; set; } = "";
        public string AckCode { get; set; } = "Accepted";
        public string Description { get; set; } = "";
        public string RawAck { get; set; } = "";
    }
}