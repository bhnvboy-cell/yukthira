using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class EdiAs2Handler : IEdiAs2Handler
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<EdiAs2Handler> _logger;

    public EdiAs2Handler(YuktiraDbContext db, ILogger<EdiAs2Handler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<EdiMessageResult> ProcessIncomingMessageAsync(EdiIncomingMessage message)
    {
        try
        {
            _logger.LogInformation(
                "Processing incoming EDI message: Type={Type}, Protocol={Protocol}, Sender={Sender}, Receiver={Receiver}",
                message.MessageType, message.Protocol, message.SenderId, message.ReceiverId);

            if (message.SecurityLevel == EdiSecurityLevel.Sign ||
                message.SecurityLevel == EdiSecurityLevel.SignAndEncrypt)
            {
                if (!await ValidateSignatureAsync(message))
                {
                    return new EdiMessageResult
                    {
                        Success = false,
                        Status = EdiTransactionStatus.Rejected,
                        ErrorMessage = "S/MIME signature validation failed"
                    };
                }
            }

            if (message.SecurityLevel == EdiSecurityLevel.Encrypt ||
                message.SecurityLevel == EdiSecurityLevel.SignAndEncrypt)
            {
                message.RawPayload = await DecryptPayloadAsync(message.RawPayload);
            }

            if (!string.IsNullOrEmpty(message.MicValue))
            {
                var computedMic = ComputeMic(message.RawPayload);
                if (!string.Equals(computedMic, message.MicValue, StringComparison.OrdinalIgnoreCase))
                {
                    return new EdiMessageResult
                    {
                        Success = false,
                        Status = EdiTransactionStatus.Rejected,
                        ErrorMessage = $"MIC mismatch: Expected={message.MicValue}, Computed={computedMic}"
                    };
                }
            }

            var result = message.MessageType switch
            {
                EdiMessageType.PurchaseOrder => await ProcessEdi850IncomingAsync(message),
                EdiMessageType.Invoice => await ProcessEdi810IncomingAsync(message),
                _ => new EdiMessageResult
                {
                    Success = true,
                    MessageId = message.Id,
                    Status = EdiTransactionStatus.Processed
                }
            };

            await LogTransmissionAsync(message.Id, message.MessageType, message.Protocol,
                result.Status, message.SenderId, message.ReceiverId, result.ErrorMessage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incoming EDI message {MessageId}", message.Id);
            await LogTransmissionAsync(message.Id, message.MessageType, message.Protocol,
                EdiTransactionStatus.Error, message.SenderId, message.ReceiverId, ex.Message);
            return new EdiMessageResult
            {
                Success = false,
                Status = EdiTransactionStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<EdiMessageResult> SendOutboundMessageAsync(EdiOutboundMessage message)
    {
        try
        {
            _logger.LogInformation(
                "Sending outbound EDI message: Type={Type}, Protocol={Protocol}, Sender={Sender}, Receiver={Receiver}",
                message.MessageType, message.Protocol, message.SenderId, message.ReceiverId);

            var payload = message.Payload;

            if (message.SecurityLevel == EdiSecurityLevel.Encrypt ||
                message.SecurityLevel == EdiSecurityLevel.SignAndEncrypt)
            {
                payload = await EncryptPayloadAsync(payload);
            }

            if (message.SecurityLevel == EdiSecurityLevel.Sign ||
                message.SecurityLevel == EdiSecurityLevel.SignAndEncrypt)
            {
                payload = await SignPayloadAsync(payload);
            }

            var mic = ComputeMic(payload);

            var headers = new Dictionary<string, string>(message.Headers)
            {
                ["AS2-Version"] = "1.2",
                ["Message-ID"] = $"<{message.Id}@yuktira-erp>",
                ["Subject"] = $"EDI {message.MessageType}",
                ["From"] = message.SenderId,
                ["To"] = message.ReceiverId,
                ["As2-From"] = message.SenderId,
                ["As2-To"] = message.ReceiverId,
                ["Disposition-Notification-To"] = message.SenderId,
                ["Receipt-Delivery-Option"] = "async",
                ["Micalg"] = "sha-256",
                ["Content-Type"] = message.ContentType ?? "application/edi-x12"
            };

            await LogTransmissionAsync(message.Id, message.MessageType, message.Protocol,
                EdiTransactionStatus.Acknowledged, message.SenderId, message.ReceiverId, null);

            return new EdiMessageResult
            {
                Success = true,
                MessageId = message.Id,
                Status = EdiTransactionStatus.Acknowledged,
                AcknowledgmentId = mic
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send outbound EDI message {MessageId}", message.Id);
            return new EdiMessageResult
            {
                Success = false,
                Status = EdiTransactionStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<MdnReceipt> GenerateMdnAsync(EdiMdnRequest request)
    {
        var mdn = new MdnReceipt
        {
            OriginalMessageId = request.MessageId,
            Status = request.Status,
            ReceiptTimestamp = DateTime.UtcNow.ToString("O"),
            MicValue = request.MicValue
        };

        if (request.Status == MdnStatus.Processed)
        {
            var mdnPayload = GenerateMdnXml(request.MessageId, request.MicValue ?? "", "processed");
            mdn.MdnPayload = mdnPayload;
        }
        else if (request.Status == MdnStatus.Failed)
        {
            var mdnPayload = GenerateMdnXml(request.MessageId, "", "failed", request.FailureReason);
            mdn.MdnPayload = mdnPayload;
            mdn.ErrorMessage = request.FailureReason;
        }

        _logger.LogInformation("MDN generated: MessageId={MessageId}, Status={Status}",
            request.MessageId, request.Status);

        return mdn;
    }

    public async Task<EdiTransmissionLog> GetTransmissionStatusAsync(Guid messageId)
    {
        return new EdiTransmissionLog
        {
            Id = messageId,
            Status = EdiTransactionStatus.Processed,
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<bool> ValidateSignatureAsync(EdiIncomingMessage message)
    {
        try
        {
            var payloadBytes = Encoding.UTF8.GetBytes(message.RawPayload);
            var signedCms = new SignedCms();
            signedCms.Decode(payloadBytes);
            signedCms.CheckSignature(true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "S/MIME signature validation failed for message {MessageId}", message.Id);
            return false;
        }
    }

    private async Task<string> DecryptPayloadAsync(string encryptedPayload)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedPayload);
            var envelopedCms = new EnvelopedCms();
            envelopedCms.Decode(encryptedBytes);
            envelopedCms.Decrypt();
            return Encoding.UTF8.GetString(envelopedCms.ContentInfo.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payload decryption failed");
            throw new InvalidOperationException("Failed to decrypt AS2 payload", ex);
        }
    }

    private async Task<string> EncryptPayloadAsync(string payload)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var contentInfo = new ContentInfo(payloadBytes);
        var envelopedCms = new EnvelopedCms(contentInfo);

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

        if (certs.Count > 0)
        {
            var recipient = new CmsRecipient(certs[0]);
            envelopedCms.Encrypt(recipient);
        }

        return Convert.ToBase64String(envelopedCms.Encode());
    }

    private async Task<string> SignPayloadAsync(string payload)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var contentInfo = new ContentInfo(payloadBytes);
        var signedCms = new SignedCms(contentInfo, true);

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

        if (certs.Count > 0)
        {
            var signer = new CmsSigner(certs[0]);
            signedCms.ComputeSignature(signer);
        }

        return Convert.ToBase64String(signedCms.Encode());
    }

    private static string ComputeMic(string payload)
    {
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(payloadBytes);
        return Convert.ToBase64String(hash);
    }

    private string GenerateMdnXml(string messageId, string mic, string status, string? failureReason = null)
    {
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.AppendLine("<Disposition-Notification>");
        xml.AppendLine($"  <Message-ID>{messageId}</Message-ID>");
        xml.AppendLine($"  <Disposition>{status}</Disposition>");
        if (!string.IsNullOrEmpty(mic))
            xml.AppendLine($"  <MIC>{mic}</MIC>");
        xml.AppendLine($"  <Timestamp>{DateTime.UtcNow:O}</Timestamp>");
        if (!string.IsNullOrEmpty(failureReason))
            xml.AppendLine($"  <Failure-Reason>{failureReason}</Failure-Reason>");
        xml.AppendLine("</Disposition-Notification>");
        return xml.ToString();
    }

    private async Task<EdiMessageResult> ProcessEdi850IncomingAsync(EdiIncomingMessage message)
    {
        try
        {
            var edi850 = JsonSerializer.Deserialize<Edi850PurchaseOrder>(message.RawPayload);
            if (edi850 == null)
                return new EdiMessageResult { Success = false, Status = EdiTransactionStatus.Rejected, ErrorMessage = "Invalid EDI 850 payload" };

            return new EdiMessageResult
            {
                Success = true,
                MessageId = message.Id,
                Status = EdiTransactionStatus.Translated,
                AcknowledgmentId = $"ACK-850-{edi850.PoNumber}"
            };
        }
        catch (Exception ex)
        {
            return new EdiMessageResult { Success = false, Status = EdiTransactionStatus.Error, ErrorMessage = ex.Message };
        }
    }

    private async Task<EdiMessageResult> ProcessEdi810IncomingAsync(EdiIncomingMessage message)
    {
        try
        {
            var edi810 = JsonSerializer.Deserialize<Edi810Invoice>(message.RawPayload);
            if (edi810 == null)
                return new EdiMessageResult { Success = false, Status = EdiTransactionStatus.Rejected, ErrorMessage = "Invalid EDI 810 payload" };

            return new EdiMessageResult
            {
                Success = true,
                MessageId = message.Id,
                Status = EdiTransactionStatus.Translated,
                AcknowledgmentId = $"ACK-810-{edi810.InvoiceNumber}"
            };
        }
        catch (Exception ex)
        {
            return new EdiMessageResult { Success = false, Status = EdiTransactionStatus.Error, ErrorMessage = ex.Message };
        }
    }

    private async Task LogTransmissionAsync(
        Guid messageId, EdiMessageType messageType, EdiTransportProtocol protocol,
        EdiTransactionStatus status, string? senderId, string? receiverId, string? error)
    {
        _logger.LogInformation(
            "EDI Transmission Log: MessageId={MessageId}, Type={Type}, Protocol={Protocol}, Status={Status}, Error={Error}",
            messageId, messageType, protocol, status, error ?? "None");
    }
}

public class EdiTransactionProcessor : IEdiTransactionProcessor
{
    private readonly YuktiraDbContext _db;
    private readonly ILogger<EdiTransactionProcessor> _logger;

    public EdiTransactionProcessor(YuktiraDbContext db, ILogger<EdiTransactionProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<EdiPurchaseOrderResult> ProcessEdi850Async(Edi850PurchaseOrder order, Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Processing EDI 850: PO={PoNumber}, Lines={LineCount}",
                order.PoNumber, order.LineItems.Count);

            var vendor = await _db.Vendors.FirstOrDefaultAsync(v =>
                v.Code == order.SellerId);

            if (vendor == null)
            {
                return new EdiPurchaseOrderResult
                {
                    Success = false,
                    Errors = { $"Vendor not found: {order.SellerId}" }
                };
            }

            var po = new PurchaseOrderEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PoNumber = order.PoNumber,
                VendorCode = vendor.Code,
                VendorName = vendor.Name,
                Date = order.PoDate,
                Status = "Created",
                TotalAmount = order.LineItems.Sum(l => l.Quantity * l.UnitPrice),
                ItemCount = order.LineItems.Count,
                CreatedAt = DateTime.UtcNow
            };

            _db.PurchaseOrders.Add(po);

            foreach (var line in order.LineItems)
            {
                var poItem = new PurchaseOrderItemEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PurchaseOrderId = po.Id,
                    LineNumber = line.LineNumber,
                    MaterialCode = line.ItemCode,
                    MaterialName = line.Description,
                    Quantity = line.Quantity,
                    UOM = line.UnitOfMeasure,
                    UnitPrice = line.UnitPrice,
                    TotalPrice = line.Quantity * line.UnitPrice,
                    DeliveryDate = line.RequestedDeliveryDate.ToString("yyyy-MM-dd"),
                    CreatedAt = DateTime.UtcNow
                };
                _db.PurchaseOrderItems.Add(poItem);
            }

            await _db.SaveChangesAsync();

            return new EdiPurchaseOrderResult
            {
                Success = true,
                PurchaseOrderNumber = po.PoNumber,
                VendorCode = vendor.Code,
                LineItemCount = order.LineItems.Count,
                TotalAmount = po.TotalAmount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process EDI 850: PO={PoNumber}", order.PoNumber);
            return new EdiPurchaseOrderResult { Success = false, Errors = { ex.Message } };
        }
    }

    public async Task<EdiInvoiceResult> ProcessEdi810Async(Edi810Invoice invoice, Guid tenantId)
    {
        try
        {
            _logger.LogInformation("Processing EDI 810: Invoice={InvoiceNumber}, Amount={Amount}",
                invoice.InvoiceNumber, invoice.TotalAmount);

            var vendor = await _db.Vendors.FirstOrDefaultAsync(v =>
                v.Code == invoice.VendorId);

            if (vendor == null)
            {
                return new EdiInvoiceResult
                {
                    Success = false,
                    Errors = { $"Vendor not found: {invoice.VendorId}" }
                };
            }

            _logger.LogInformation("EDI 810 invoice processed: Invoice={InvoiceNumber}, Vendor={VendorCode}",
                invoice.InvoiceNumber, vendor.Code);

            return new EdiInvoiceResult
            {
                Success = true,
                InvoiceNumber = invoice.InvoiceNumber,
                VendorCode = vendor.Code,
                CustomerCode = invoice.CustomerId,
                TotalAmount = invoice.TotalAmount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process EDI 810: Invoice={InvoiceNumber}", invoice.InvoiceNumber);
            return new EdiInvoiceResult { Success = false, Errors = { ex.Message } };
        }
    }

    public Task<List<EdiTransactionSummary>> GetPendingTransactionsAsync(Guid tenantId)
    {
        return Task.FromResult(new List<EdiTransactionSummary>());
    }
}
