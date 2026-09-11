using YuktiraERP.Core.Dtos;

namespace YuktiraERP.Core.Interfaces;

public interface IEdiAs2Handler
{
    Task<EdiMessageResult> ProcessIncomingMessageAsync(EdiIncomingMessage message);
    Task<EdiMessageResult> SendOutboundMessageAsync(EdiOutboundMessage message);
    Task<MdnReceipt> GenerateMdnAsync(EdiMdnRequest request);
    Task<EdiTransmissionLog> GetTransmissionStatusAsync(Guid messageId);
}

public interface IEdiTransactionProcessor
{
    Task<EdiPurchaseOrderResult> ProcessEdi850Async(Edi850PurchaseOrder order, Guid tenantId);
    Task<EdiInvoiceResult> ProcessEdi810Async(Edi810Invoice invoice, Guid tenantId);
    Task<List<EdiTransactionSummary>> GetPendingTransactionsAsync(Guid tenantId);
}
