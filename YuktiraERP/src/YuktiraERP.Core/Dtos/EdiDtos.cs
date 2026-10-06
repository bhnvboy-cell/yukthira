using YuktiraERP.Core.Enums;

namespace YuktiraERP.Core.Dtos;

public class EdiIncomingMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EdiMessageType MessageType { get; set; }
    public EdiTransportProtocol Protocol { get; set; }
    public EdiSecurityLevel SecurityLevel { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string RawPayload { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? MessageId { get; set; }
    public string? MicValue { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class EdiOutboundMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EdiMessageType MessageType { get; set; }
    public EdiTransportProtocol Protocol { get; set; }
    public EdiSecurityLevel SecurityLevel { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class EdiMessageResult
{
    public bool Success { get; set; }
    public Guid MessageId { get; set; }
    public EdiTransactionStatus Status { get; set; }
    public string? AcknowledgmentId { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

public class MdnReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalMessageId { get; set; } = string.Empty;
    public MdnStatus Status { get; set; }
    public string? MicValue { get; set; }
    public string? ReceiptTimestamp { get; set; }
    public string? MdnPayload { get; set; }
    public string? ErrorMessage { get; set; }
}

public class EdiMdnRequest
{
    public string MessageId { get; set; } = string.Empty;
    public MdnStatus Status { get; set; }
    public string? MicValue { get; set; }
    public string? FailureReason { get; set; }
}

public class EdiTransmissionLog
{
    public Guid Id { get; set; }
    public EdiMessageType MessageType { get; set; }
    public EdiTransportProtocol Protocol { get; set; }
    public EdiTransactionStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string? SenderId { get; set; }
    public string? ReceiverId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class Edi850PurchaseOrder
{
    public string PoNumber { get; set; } = string.Empty;
    public DateTime PoDate { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public List<Edi850LineItem> LineItems { get; set; } = new();
    public Edi850ShipTo? ShipTo { get; set; }
}

public class Edi850LineItem
{
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public DateTime RequestedDeliveryDate { get; set; }
}

public class Edi850ShipTo
{
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class EdiPurchaseOrderResult
{
    public bool Success { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? VendorCode { get; set; }
    public int LineItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class Edi810Invoice
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string VendorId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? PoNumber { get; set; }
    public List<Edi810LineItem> LineItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

public class Edi810LineItem
{
    public int LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class EdiInvoiceResult
{
    public bool Success { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? VendorCode { get; set; }
    public string? CustomerCode { get; set; }
    public decimal TotalAmount { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class EdiTransactionSummary
{
    public Guid Id { get; set; }
    public EdiMessageType MessageType { get; set; }
    public EdiTransactionStatus Status { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class EdiTransmissionQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Direction { get; set; }
    public EdiTransactionStatus? Status { get; set; }
    public EdiMessageType? MessageType { get; set; }
    public string? Search { get; set; }
}

public class EdiTransmissionEntry
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Direction { get; set; } = string.Empty;
    public EdiMessageType MessageType { get; set; }
    public EdiTransportProtocol Protocol { get; set; }
    public EdiTransactionStatus Status { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string PartnerCode { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? AcknowledgmentId { get; set; }
}

public class EdiTransmissionDetail : EdiTransmissionEntry
{
    public string? RawPayload { get; set; }
}

public class EdiTransmissionPageResult
{
    public List<EdiTransmissionEntry> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class EdiTransmissionStatsResult
{
    public int Total { get; set; }
    public int Inbound { get; set; }
    public int Outbound { get; set; }
    public int Failed { get; set; }
    public Dictionary<string, int> ByDirection { get; set; } = new();
    public Dictionary<string, int> ByStatus { get; set; } = new();
}

public class EdiTransmissionLogEntry
{
    public string Direction { get; set; } = string.Empty;
    public EdiMessageType? MessageType { get; set; }
    public EdiTransportProtocol Protocol { get; set; } = EdiTransportProtocol.As2;
    public EdiTransactionStatus Status { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string PartnerCode { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? RawPayload { get; set; }
    public string? AcknowledgmentId { get; set; }
    public string? ErrorMessage { get; set; }
}
