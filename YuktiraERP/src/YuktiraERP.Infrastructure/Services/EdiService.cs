using System.Text;
using System.Text.Json;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.Services;

public class EdiService : IEdiService
{
    private const string SegmentTerminator = "'";
    private const string ElementSeparator = "+";
    private const char DataSeparator = ':';

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