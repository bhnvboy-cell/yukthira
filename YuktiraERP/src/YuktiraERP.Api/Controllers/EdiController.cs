using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/integration/edi")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class EdiController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly IEdiService _edi;
    private readonly ITenantContext _tenant;
    private readonly ILogger<EdiController> _logger;

    public EdiController(YuktiraDbContext db, IEdiService edi, ITenantContext tenant, ILogger<EdiController> logger)
    {
        _db = db;
        _edi = edi;
        _tenant = tenant;
        _logger = logger;
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
        var isX12 = standard.ToUpperInvariant() == "X12";
        var messageType = _edi.ParseMessageType(documentType);
        var documentLabel = messageType.HasValue ? _edi.GetDocumentTypeLabel(messageType.Value) : documentType;
        var (senderId, receiverId, partnerCode) = await ResolveInterchangePartiesAsync(payload);

        try
        {
            var result = isX12
                ? await _edi.ConvertToX12Async(payload, documentType)
                : await _edi.ConvertToEdifactAsync(payload, documentType);

            await LogTransmissionSafeAsync(new EdiTransmissionLogEntry
            {
                Direction = "Outbound",
                MessageType = messageType,
                Protocol = EdiTransportProtocol.As2,
                Status = EdiTransactionStatus.Processed,
                SenderId = senderId,
                ReceiverId = receiverId,
                PartnerCode = partnerCode,
                DocumentType = documentLabel,
                RawPayload = result
            });

            return Ok(new { standard, documentType, content = result });
        }
        catch (ArgumentException ex)
        {
            await LogTransmissionSafeAsync(new EdiTransmissionLogEntry
            {
                Direction = "Outbound",
                MessageType = messageType,
                Protocol = EdiTransportProtocol.As2,
                Status = EdiTransactionStatus.Error,
                SenderId = senderId,
                ReceiverId = receiverId,
                PartnerCode = partnerCode,
                DocumentType = documentLabel,
                ErrorMessage = ex.Message
            });

            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("parse/{standard}")]
    public async Task<IActionResult> Parse(string standard, [FromBody] ParseRequest req)
    {
        var content = req?.Content ?? "";
        var isX12 = standard.ToUpperInvariant() == "X12";
        try
        {
            var result = isX12
                ? await _edi.ParseX12Async(content)
                : await _edi.ParseEdifactAsync(content);

            var detected = ExtractDetectedMessageType(result);
            var messageType = _edi.ParseMessageType(detected);
            var (senderId, receiverId) = ExtractPartiesFromPayload(content, isX12);
            var partnerCode = await MatchPartnerBySenderIdAsync(senderId);

            await LogTransmissionSafeAsync(new EdiTransmissionLogEntry
            {
                Direction = "Inbound",
                MessageType = messageType,
                Protocol = EdiTransportProtocol.As2,
                Status = EdiTransactionStatus.Translated,
                SenderId = senderId,
                ReceiverId = receiverId,
                PartnerCode = partnerCode,
                DocumentType = messageType.HasValue ? _edi.GetDocumentTypeLabel(messageType.Value) : (detected ?? ""),
                RawPayload = content
            });

            return Ok(new { standard, parsed = result });
        }
        catch (ArgumentException ex)
        {
            await LogTransmissionSafeAsync(new EdiTransmissionLogEntry
            {
                Direction = "Inbound",
                MessageType = null,
                Protocol = EdiTransportProtocol.As2,
                Status = EdiTransactionStatus.Error,
                SenderId = "",
                ReceiverId = "",
                PartnerCode = "",
                DocumentType = "",
                RawPayload = content,
                ErrorMessage = ex.Message
            });

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

    // ── Interchange Log ──

    [HttpGet("transmissions")]
    public async Task<IActionResult> GetTransmissions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? direction = null,
        [FromQuery] string? status = null,
        [FromQuery] string? messageType = null,
        [FromQuery] string? search = null)
    {
        var result = await _edi.GetTransmissionsAsync(_tenant.TenantId, new EdiTransmissionQuery
        {
            Page = page,
            PageSize = pageSize,
            Direction = NormalizeDirection(direction),
            Status = ParseStatusFilter(status),
            MessageType = ParseMessageTypeFilter(messageType),
            Search = search
        });

        return Ok(new { items = result.Items, totalCount = result.TotalCount, page = result.Page, pageSize = result.PageSize });
    }

    [HttpGet("transmissions/{id}")]
    public async Task<IActionResult> GetTransmission(Guid id)
    {
        var detail = await _edi.GetTransmissionAsync(_tenant.TenantId, id);
        return detail == null ? NotFound() : Ok(detail);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetTransmissionStats()
    {
        var stats = await _edi.GetTransmissionStatsAsync(_tenant.TenantId);
        return Ok(new
        {
            total = stats.Total,
            inbound = stats.Inbound,
            outbound = stats.Outbound,
            failed = stats.Failed,
            byDirection = stats.ByDirection,
            byStatus = stats.ByStatus
        });
    }

    // ── Interchange log helpers ──

    private async Task LogTransmissionSafeAsync(EdiTransmissionLogEntry entry)
    {
        try
        {
            await _edi.LogTransmissionAsync(_tenant.TenantId, entry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to record EDI transmission log entry");
        }
    }

    private async Task<(string SenderId, string ReceiverId, string PartnerCode)> ResolveInterchangePartiesAsync(object payload)
    {
        var partnerCode = GetPayloadString(payload, "PartnerCode") ?? "";
        var sender = GetPayloadString(payload, "Sender") ?? "";
        var receiver = GetPayloadString(payload, "Receiver") ?? "";

        if (!string.IsNullOrWhiteSpace(partnerCode))
        {
            var partner = await _db.EdiTradingPartners
                .FirstOrDefaultAsync(p => p.TenantId == _tenant.TenantId && p.PartnerCode == partnerCode);
            if (partner != null)
                return (partner.SenderId, partner.ReceiverId, partner.PartnerCode);
        }

        return (
            string.IsNullOrWhiteSpace(sender) ? "YUKTIRA" : sender,
            string.IsNullOrWhiteSpace(receiver) ? "PARTNER" : receiver,
            partnerCode);
    }

    private static string? GetPayloadString(object payload, string propertyName)
    {
        if (payload is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString()
                        : property.Value.ToString();
            }
        }
        return null;
    }

    private static (string SenderId, string ReceiverId) ExtractPartiesFromPayload(string content, bool isX12)
    {
        if (string.IsNullOrWhiteSpace(content)) return ("", "");

        if (isX12)
        {
            var isaIndex = content.IndexOf("ISA*", StringComparison.Ordinal);
            var segment = isaIndex >= 0 ? content.Substring(isaIndex) : content;
            var end = segment.IndexOf('~');
            if (end > 0) segment = segment.Substring(0, end);
            var fields = segment.Split('*');
            if (fields.Length > 8)
                return (fields[6].Trim(), fields[8].Trim());
        }
        else
        {
            foreach (var piece in content.Split('\''))
            {
                var trimmed = piece.Trim().TrimStart('\n', '\r', ' ');
                if (!trimmed.StartsWith("UNB+", StringComparison.Ordinal)) continue;
                var fields = trimmed.Split('+');
                if (fields.Length > 3)
                    return (fields[2].Trim(), fields[3].Trim());
                break;
            }
        }

        return ("", "");
    }

    private async Task<string> MatchPartnerBySenderIdAsync(string senderId)
    {
        if (string.IsNullOrWhiteSpace(senderId)) return "";
        var partner = await _db.EdiTradingPartners
            .FirstOrDefaultAsync(p => p.TenantId == _tenant.TenantId && p.SenderId == senderId);
        return partner?.PartnerCode ?? "";
    }

    private static string? ExtractDetectedMessageType(object parsed)
    {
        if (parsed is IDictionary<string, object> dictionary
            && dictionary.TryGetValue("MessageType", out var value))
        {
            return value?.ToString();
        }
        return null;
    }

    private static string? NormalizeDirection(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction)) return null;
        var value = direction.Trim();
        if (value.Equals("Inbound", StringComparison.OrdinalIgnoreCase)) return "Inbound";
        if (value.Equals("Outbound", StringComparison.OrdinalIgnoreCase)) return "Outbound";
        return value;
    }

    private static EdiTransactionStatus? ParseStatusFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var numeric)) return (EdiTransactionStatus)numeric;
        return Enum.TryParse<EdiTransactionStatus>(value, true, out var parsed) ? parsed : null;
    }

    private static EdiMessageType? ParseMessageTypeFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var numeric)) return (EdiMessageType)numeric;
        return Enum.TryParse<EdiMessageType>(value, true, out var parsed) ? parsed : null;
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