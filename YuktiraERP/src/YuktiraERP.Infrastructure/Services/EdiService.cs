using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class EdiService : IEdiService
{
    private const string SegmentTerminator = "'";
    private const string ElementSeparator = "+";
    private const char DataSeparator = ':';

    private readonly YuktiraDbContext? _db;
    private readonly ITenantContext? _tenant;

    public EdiService(YuktiraDbContext? db = null, ITenantContext? tenant = null)
    {
        _db = db;
        _tenant = tenant;
    }

    public Task<string> ConvertToEdifactAsync(object data, string documentType)
    {
        var doc = Normalize(data);
        var sb = new StringBuilder();

        var messageRef = doc.GetString("MessageReference", Guid.NewGuid().ToString("N")[..14]);
        var sender = doc.GetString("Sender", "YUKTIRA");
        var receiver = doc.GetString("Receiver", "PARTNER");
        var issueDate = DateTime.UtcNow;
        var docDate = doc.GetDate("Date") ?? issueDate;

        // UNA (service string advice)
        sb.AppendLine("UNA:+.? '");

        // UNB interchange header
        sb.AppendLine($"UNB+UNOA:2+{sender}+{receiver}+{issueDate:yyMMdd:HHmm}+{messageRef}'");

        switch (documentType.ToUpperInvariant())
        {
            case "PO":
                sb.AppendLine(BuildPurchaseOrderEdifact(doc, messageRef, docDate));
                break;
            case "INVOICE":
                sb.AppendLine(BuildInvoiceEdifact(doc, messageRef, docDate));
                break;
            case "GRN":
                sb.AppendLine(BuildGoodsReceiptEdifact(doc, messageRef, docDate));
                break;
            default:
                throw new ArgumentException($"Unsupported EDIFACT document type: {documentType}");
        }

        sb.AppendLine($"UNZ+1+{messageRef}'");
        return Task.FromResult(sb.ToString());
    }

    public Task<string> ConvertToX12Async(object data, string documentType)
    {
        var doc = Normalize(data);
        var sb = new StringBuilder();

        var interchangeId = doc.GetString("InterchangeId", Guid.NewGuid().ToString("N")[..9]);
        var sender = doc.GetString("Sender", "YUKTIRA").Trim();
        var receiver = doc.GetString("Receiver", "PARTNER").Trim();
        var issueDate = DateTime.UtcNow;

        // ISA interchange header (ISA01..ISA16)
        sb.AppendLine($"ISA*00*          *00*          *ZZ*{sender,-15}*ZZ*{receiver,-15}*{issueDate:yyMMdd}*{issueDate:HHmm}*U*00401*{interchangeId}*0*P*>~");

        switch (documentType.ToUpperInvariant())
        {
            case "PO":
                sb.AppendLine(BuildPurchaseOrderX12(doc, interchangeId));
                break;
            case "INVOICE":
                sb.AppendLine(BuildInvoiceX12(doc, interchangeId));
                break;
            case "GRN":
                sb.AppendLine(BuildGoodsReceiptX12(doc, interchangeId));
                break;
            default:
                throw new ArgumentException($"Unsupported X12 document type: {documentType}");
        }

        sb.AppendLine($"IEA*1*{interchangeId}~");
        return Task.FromResult(sb.ToString());
    }

    public Task<object> ParseEdifactAsync(string ediContent)
    {
        var segments = ediContent
            .Split(SegmentTerminator, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().TrimStart('\n', '\r'))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var result = new Dictionary<string, object>
        {
            ["MessageType"] = DetectEdifactMessage(segments),
            ["Segments"] = segments.Count
        };

        var lineItems = new List<object>();
        foreach (var seg in segments)
        {
            var name = seg[..Math.Min(3, seg.Length)];
            if (name == "LIN")
            {
                var parts = seg.Split(ElementSeparator);
                lineItems.Add(new Dictionary<string, object>
                {
                    ["LineNumber"] = parts.Length > 1 ? parts[1] : "",
                    ["ItemCode"] = parts.Length > 3 && parts[3].Contains(':') ? parts[3].Split(':')[0] : (parts.Length > 3 ? parts[3] : "")
                });
            }
            else if (name == "BGM")
            {
                var parts = seg.Split(ElementSeparator);
                if (parts.Length > 2) result["OrderNumber"] = parts[2];
            }
            else if (name == "QTY")
            {
                var parts = seg.Split(ElementSeparator);
                var qtyParts = (parts.Length > 1 ? parts[1] : "").Split(':');
                result["Quantity"] = qtyParts.Length > 1 ? qtyParts[1] : (qtyParts.Length > 0 ? qtyParts[0] : "");
            }
            else if (name == "MOA")
            {
                var parts = seg.Split(ElementSeparator);
                var amountParts = (parts.Length > 1 ? parts[1] : "").Split(':');
                if (amountParts.Length > 0) result["Amount"] = amountParts[0];
                if (amountParts.Length > 1) result["Currency"] = amountParts[1];
            }
            else if (name == "DTM")
            {
                var parts = seg.Split(ElementSeparator);
                var dtmParts = (parts.Length > 1 ? parts[1] : "").Split(':');
                if (dtmParts.Length > 1) result["DocumentDate"] = dtmParts[1];
            }
        }

        if (lineItems.Count > 0) result["LineItems"] = lineItems;
        return Task.FromResult<object>(result);
    }

    public Task<object> ParseX12Async(string ediContent)
    {
        var lines = ediContent
            .Split('~', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var result = new Dictionary<string, object>
        {
            ["MessageType"] = DetectX12Transaction(lines),
            ["Segments"] = lines.Count
        };

        var lineItems = new List<object>();
        foreach (var line in lines)
        {
            var seg = line.Split('*');
            var name = seg[0].Trim();
            switch (name)
            {
                case "BEG":
                    if (seg.Length > 3) result["OrderNumber"] = seg[3];
                    if (seg.Length > 4) result["OrderDate"] = seg[4];
                    break;
                case "PO1":
                    lineItems.Add(new Dictionary<string, object>
                    {
                        ["LineNumber"] = seg.Length > 1 ? seg[1] : "",
                        ["Quantity"] = seg.Length > 2 ? seg[2] : "",
                        ["UnitPrice"] = seg.Length > 4 ? seg[4] : "",
                        ["ItemCode"] = seg.Length > 7 ? seg[7] : ""
                    });
                    break;
                case "IT1":
                    lineItems.Add(new Dictionary<string, object>
                    {
                        ["LineNumber"] = seg.Length > 1 ? seg[1] : "",
                        ["Quantity"] = seg.Length > 2 ? seg[2] : "",
                        ["UnitPrice"] = seg.Length > 4 ? seg[4] : "",
                        ["ItemCode"] = seg.Length > 7 ? seg[7] : ""
                    });
                    break;
                case "CTT":
                    if (seg.Length > 1) result["LineItemCount"] = seg[1];
                    break;
                case "TDS":
                    if (seg.Length > 1) result["TotalAmount"] = seg[1];
                    break;
                case "DTM":
                    if (seg.Length > 2) result["DocumentDate"] = seg[2];
                    break;
            }
        }

        if (lineItems.Count > 0) result["LineItems"] = lineItems;
        return Task.FromResult<object>(result);
    }

    // ── Message type mapping ──

    public EdiMessageType? ParseMessageType(string? documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType)) return null;

        var key = documentType.Trim().ToUpperInvariant();
        switch (key)
        {
            case "PO":
            case "PURCHASEORDER":
            case "850":
            case "ORDERS":
                return EdiMessageType.PurchaseOrder;
            case "INVOICE":
            case "810":
            case "INVOIC":
                return EdiMessageType.Invoice;
            case "GRN":
            case "856":
            case "DESADV":
            case "RECADV":
                return EdiMessageType.AdvanceShipNotice;
            case "855":
            case "ORDRSP":
                return EdiMessageType.PurchaseOrderAck;
            case "997":
            case "CONTRL":
                return EdiMessageType.FunctionalAck;
        }

        return Enum.TryParse(key, true, out EdiMessageType messageType) ? messageType : null;
    }

    public string GetDocumentTypeLabel(EdiMessageType messageType)
    {
        switch (messageType)
        {
            case EdiMessageType.PurchaseOrder: return "850/ORDERS";
            case EdiMessageType.AdvanceShipNotice: return "856/DESADV";
            case EdiMessageType.Invoice: return "810/INVOIC";
            case EdiMessageType.PurchaseOrderAck: return "855/ORDRSP";
            case EdiMessageType.FunctionalAck: return "997/CONTRL";
            default: return messageType.ToString();
        }
    }

    // ── Interchange log ──

    public async Task<EdiTransmissionPageResult> GetTransmissionsAsync(Guid tenantId, EdiTransmissionQuery query)
    {
        var db = RequireDb();

        IQueryable<EdiTransmissionEntity> q = db.EdiTransmissions.Where(t => t.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(query.Direction))
        {
            var direction = query.Direction.Trim();
            q = q.Where(t => t.Direction == direction);
        }
        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            q = q.Where(t => t.Status == status);
        }
        if (query.MessageType.HasValue)
        {
            var messageType = query.MessageType.Value;
            q = q.Where(t => t.MessageType == messageType);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            q = q.Where(t => t.SenderId.Contains(search)
                || t.ReceiverId.Contains(search)
                || t.PartnerCode.Contains(search)
                || t.DocumentType.Contains(search));
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 200);
        var totalCount = await q.CountAsync();
        var items = await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new EdiTransmissionEntry
            {
                Id = t.Id,
                CreatedAt = t.CreatedAt,
                Direction = t.Direction,
                MessageType = t.MessageType,
                Protocol = t.Protocol,
                Status = t.Status,
                SenderId = t.SenderId,
                ReceiverId = t.ReceiverId,
                PartnerCode = t.PartnerCode,
                DocumentType = t.DocumentType,
                ErrorMessage = t.ErrorMessage,
                AcknowledgmentId = t.AcknowledgmentId
            })
            .ToListAsync();

        return new EdiTransmissionPageResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EdiTransmissionDetail?> GetTransmissionAsync(Guid tenantId, Guid id)
    {
        var db = RequireDb();
        var transmission = await db.EdiTransmissions
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        if (transmission == null) return null;

        return new EdiTransmissionDetail
        {
            Id = transmission.Id,
            CreatedAt = transmission.CreatedAt,
            Direction = transmission.Direction,
            MessageType = transmission.MessageType,
            Protocol = transmission.Protocol,
            Status = transmission.Status,
            SenderId = transmission.SenderId,
            ReceiverId = transmission.ReceiverId,
            PartnerCode = transmission.PartnerCode,
            DocumentType = transmission.DocumentType,
            ErrorMessage = transmission.ErrorMessage,
            AcknowledgmentId = transmission.AcknowledgmentId,
            RawPayload = transmission.RawPayload
        };
    }

    public async Task<EdiTransmissionStatsResult> GetTransmissionStatsAsync(Guid tenantId)
    {
        var db = RequireDb();
        var q = db.EdiTransmissions.Where(t => t.TenantId == tenantId);

        var stats = new EdiTransmissionStatsResult
        {
            Total = await q.CountAsync(),
            Inbound = await q.CountAsync(t => t.Direction == "Inbound"),
            Outbound = await q.CountAsync(t => t.Direction == "Outbound"),
            Failed = await q.CountAsync(t => t.Status == EdiTransactionStatus.Error || t.Status == EdiTransactionStatus.Rejected)
        };

        var byDirection = await q
            .GroupBy(t => t.Direction)
            .Select(g => new { Direction = g.Key, Count = g.Count() })
            .ToListAsync();
        foreach (var group in byDirection)
            stats.ByDirection[group.Direction ?? ""] = group.Count;

        var byStatus = await q
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        foreach (var group in byStatus)
            stats.ByStatus[group.Status.ToString()] = group.Count;

        return stats;
    }

    public async Task LogTransmissionAsync(Guid tenantId, EdiTransmissionLogEntry entry)
    {
        var db = RequireDb();
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        db.EdiTransmissions.Add(new EdiTransmissionEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            TenantId = tenantId != Guid.Empty ? tenantId : (_tenant?.TenantId ?? Guid.Empty),
            Direction = entry.Direction,
            MessageType = entry.MessageType ?? EdiMessageType.MdReceipt,
            Protocol = entry.Protocol,
            Status = entry.Status,
            SenderId = entry.SenderId,
            ReceiverId = entry.ReceiverId,
            PartnerCode = entry.PartnerCode,
            DocumentType = entry.DocumentType,
            RawPayload = entry.RawPayload,
            AcknowledgmentId = entry.AcknowledgmentId,
            ErrorMessage = entry.ErrorMessage
        });

        await db.SaveChangesAsync();
    }

    private YuktiraDbContext RequireDb() =>
        _db ?? throw new InvalidOperationException(
            "EdiService requires a database context for interchange log operations.");

    // ── EDIFACT builders ──

    private static string BuildPurchaseOrderEdifact(EdiDocument doc, string messageRef, DateTime date)
    {
        var sb = new StringBuilder();
        sb.AppendLine("UNH+1+ORDERS:D:96A:UN'");
        sb.AppendLine($"BGM+220+{doc.GetString("OrderNumber", "PO-0001")}+9'");
        sb.AppendLine($"DTM+137:{date:yyyyMMdd}:102'");
        sb.AppendLine($"NAD+BY+{doc.GetString("VendorName", "SUPPLIER")}'");
        sb.AppendLine($"NAD+SU+{doc.GetString("CustomerName", "BUYER")}'");

        var lines = doc.GetLines();
        var index = 0;
        foreach (var line in lines)
        {
            index++;
            sb.AppendLine($"LIN+{index}++{line.GetString("ItemCode", "ITEM")}:IN'");
            sb.AppendLine($"QTY+21:{line.GetNumber("Quantity", 1)}:EA'");
            sb.AppendLine($"PRI+AAA:{line.GetNumber("UnitPrice", 0):F2}::EA'");
        }
        sb.AppendLine($"CNT+2:{index}'");
        sb.AppendLine($"UNS+S'");
        sb.AppendLine($"CNT+11:{doc.GetNumber("TotalAmount", lines.Sum(l => l.GetNumber("Total", 0))):F2}'");
        sb.AppendLine("UNT+" + (11 + index * 3) + "+1'");
        return sb.ToString();
    }

    private static string BuildInvoiceEdifact(EdiDocument doc, string messageRef, DateTime date)
    {
        var sb = new StringBuilder();
        sb.AppendLine("UNH+1+INVOIC:D:96A:UN'");
        sb.AppendLine($"BGM+380+{doc.GetString("InvoiceNumber", "INV-0001")}+9'");
        sb.AppendLine($"DTM+137:{date:yyyyMMdd}:102'");
        sb.AppendLine($"NAD+BY+{doc.GetString("CustomerName", "CUSTOMER")}'");
        sb.AppendLine($"NAD+SU+{doc.GetString("VendorName", "SUPPLIER")}'");
        sb.AppendLine($"NAD+SE+{doc.GetString("BillTo", "")}'");

        var lines = doc.GetLines();
        var index = 0;
        foreach (var line in lines)
        {
            index++;
            sb.AppendLine($"LIN+{index}++{line.GetString("ItemCode", "ITEM")}:IN'");
            sb.AppendLine($"QTY+47:{line.GetNumber("Quantity", 1)}:EA'");
            sb.AppendLine($"MOA+203:{line.GetNumber("Total", 0):F2}'");
        }
        sb.AppendLine($"MOA+77:{doc.GetNumber("TotalAmount", lines.Sum(l => l.GetNumber("Total", 0))):F2}:{doc.GetString("Currency", "USD")}'");
        sb.AppendLine($"MOA+124:{doc.GetNumber("TaxAmount", 0):F2}:{doc.GetString("Currency", "USD")}'");
        sb.AppendLine("UNT+" + (13 + index * 3) + "+1'");
        return sb.ToString();
    }

    private static string BuildGoodsReceiptEdifact(EdiDocument doc, string messageRef, DateTime date)
    {
        var sb = new StringBuilder();
        sb.AppendLine("UNH+1+RECADV:D:96A:UN'");
        sb.AppendLine($"BGM+631+{doc.GetString("GrnNumber", "GRN-0001")}+9'");
        sb.AppendLine($"DTM+137:{date:yyyyMMdd}:102'");
        sb.AppendLine($"RFF+VN:{doc.GetString("PoNumber", "")}'");
        sb.AppendLine($"NAD+BY+{doc.GetString("VendorName", "SUPPLIER")}'");

        var lines = doc.GetLines();
        var index = 0;
        foreach (var line in lines)
        {
            index++;
            sb.AppendLine($"LIN+{index}++{line.GetString("ItemCode", "ITEM")}:IN'");
            sb.AppendLine($"QTY+48:{line.GetNumber("Quantity", 1)}:EA'");
        }
        sb.AppendLine($"CNT+2:{index}'");
        sb.AppendLine("UNT+" + (9 + index * 2) + "+1'");
        return sb.ToString();
    }

    // ── X12 builders ──

    private static string BuildPurchaseOrderX12(EdiDocument doc, string interchangeId)
    {
        var sb = new StringBuilder();
        var date = doc.GetDate("Date") ?? DateTime.UtcNow;
        sb.AppendLine("ST*850*0001~");
        sb.AppendLine($"BEG*00*SA*{doc.GetString("OrderNumber", "PO-0001")}**{date:yyyyMMdd}~");
        sb.AppendLine($"N1*VN*{doc.GetString("VendorName", "SUPPLIER")}~");
        sb.AppendLine($"N1*BY*{doc.GetString("CustomerName", "BUYER")}~");

        var lines = doc.GetLines();
        var index = 0;
        foreach (var line in lines)
        {
            index++;
            sb.AppendLine($"PO1*{index}*{line.GetNumber("Quantity", 1)}*EA*{line.GetNumber("UnitPrice", 0):F2}**IN*{line.GetString("ItemCode", "ITEM")}~");
            sb.AppendLine($"PID*F****{line.GetString("Description", "")}~");
        }
        sb.AppendLine($"CTT*{index}~");
        sb.AppendLine($"SE*{8 + index * 2}*0001~");
        return sb.ToString();
    }

    private static string BuildInvoiceX12(EdiDocument doc, string interchangeId)
    {
        var sb = new StringBuilder();
        var date = doc.GetDate("Date") ?? DateTime.UtcNow;
        sb.AppendLine("ST*810*0001~");
        sb.AppendLine($"BIG*{date:yyyyMMdd}*{doc.GetString("InvoiceNumber", "INV-0001")}~");
        sb.AppendLine($"N1*BY*{doc.GetString("CustomerName", "CUSTOMER")}~");
        sb.AppendLine($"N1*SU*{doc.GetString("VendorName", "SUPPLIER")}~");

        var lines = doc.GetLines();
        var index = 0;
        foreach (var line in lines)
        {
            index++;
            sb.AppendLine($"IT1*{index}*{line.GetNumber("Quantity", 1)}*EA*{line.GetNumber("UnitPrice", 0):F2}**IN*{line.GetString("ItemCode", "ITEM")}~");
        }
        sb.AppendLine($"TDS*{doc.GetNumber("TotalAmount", lines.Sum(l => l.GetNumber("Total", 0))):F2}~");
        sb.AppendLine($"AMT*T*{doc.GetNumber("TaxAmount", 0):F2}~");
        sb.AppendLine($"CTT*{index}~");
        sb.AppendLine($"SE*{9 + index}*0001~");
        return sb.ToString();
    }

    private static string BuildGoodsReceiptX12(EdiDocument doc, string interchangeId)
    {
        var sb = new StringBuilder();
        var date = doc.GetDate("Date") ?? DateTime.UtcNow;
        sb.AppendLine("ST*861*0001~");
        sb.AppendLine($"BGN*00*{doc.GetString("GrnNumber", "GRN-0001")}*{date:yyyyMMdd}~");
        sb.AppendLine($"REF*VN*{doc.GetString("PoNumber", "")}~");

        var lines = doc.GetLines();
        var index = 0;
        foreach (var line in lines)
        {
            index++;
            sb.AppendLine($"RCD*{index}*{line.GetNumber("Quantity", 1)}*EA*{line.GetString("ItemCode", "ITEM")}~");
        }
        sb.AppendLine($"CTT*{index}~");
        sb.AppendLine($"SE*{6 + index}*0001~");
        return sb.ToString();
    }

    // ── Helpers ──

    private static string DetectEdifactMessage(List<string> segments)
    {
        foreach (var seg in segments)
        {
            if (seg.StartsWith("UNH+")) return seg.Contains("ORDERS") ? "ORDERS" : seg.Contains("INVOIC") ? "INVOIC" : seg.Contains("RECADV") ? "RECADV" : "UNKNOWN";
        }
        return "UNKNOWN";
    }

    private static string DetectX12Transaction(List<string> lines)
    {
        foreach (var line in lines)
        {
            var seg = line.Split('*');
            if (seg[0] == "ST") return seg.Length > 1 ? seg[1] : "UNKNOWN";
        }
        return "UNKNOWN";
    }

    private static EdiDocument Normalize(object data)
    {
        return data switch
        {
            EdiDocument d => d,
            string s => EdiDocument.FromJson(s),
            _ => EdiDocument.FromObject(data)
        };
    }

    private sealed class EdiDocument
    {
        private readonly JsonElement _root;
        private List<EdiDocument>? _lines;

        private EdiDocument(JsonElement root) { _root = root; }

        public static EdiDocument FromObject(object data) => new(JsonSerializer.SerializeToElement(data));

        public static EdiDocument FromJson(string json) => new(JsonDocument.Parse(json).RootElement);

        public string GetString(string key, string fallback = "")
        {
            return _root.ValueKind == JsonValueKind.Object && _root.TryGetProperty(key, out var prop)
                ? prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? fallback : prop.ToString()
                : fallback;
        }

        public decimal GetNumber(string key, decimal fallback = 0)
        {
            if (_root.ValueKind == JsonValueKind.Object && _root.TryGetProperty(key, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var num)) return num;
                if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var parsed)) return parsed;
            }
            return fallback;
        }

        public DateTime? GetDate(string key)
        {
            var value = GetString(key);
            if (string.IsNullOrEmpty(value)) return null;
            if (DateTime.TryParse(value, out var d)) return d;
            if (DateTime.TryParseExact(value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var d2)) return d2;
            return null;
        }

        public List<EdiDocument> GetLines()
        {
            if (_lines != null) return _lines;
            _lines = new List<EdiDocument>();
            if (_root.ValueKind == JsonValueKind.Object && _root.TryGetProperty("Lines", out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.EnumerateArray())
                    _lines.Add(new EdiDocument(item.Clone()));
            }
            return _lines;
        }
    }
}