using System;
using System.Collections.Generic;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Domain.Sales;

public class SalesQuotation : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string QuotationNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid BranchId { get; set; }
    public GenericDocumentState Status { get; set; } = GenericDocumentState.Draft;
    public DateOnly DocumentDate { get; set; }
    public DateOnly ValidUntilDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public decimal SubTotal { get; set; } = 0m;
    public decimal TaxAmount { get; set; } = 0m;
    public decimal TotalAmount { get; set; } = 0m;
    public string? Remarks { get; set; }

    public ICollection<SalesQuotationLine> Lines { get; set; } = new List<SalesQuotationLine>();
}

public class SalesQuotationLine : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuotationId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UomCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; set; }
}

public class SalesOrder : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid BranchId { get; set; }
    public GenericDocumentState Status { get; set; } = GenericDocumentState.Draft;
    public DateOnly DocumentDate { get; set; }
    public DateOnly DeliveryDueDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1.0m;
    public decimal SubTotal { get; set; } = 0m;
    public decimal TaxAmount { get; set; } = 0m;
    public decimal TotalAmount { get; set; } = 0m;
    public bool IsOnHold { get; set; } = false;
    public string? HoldReason { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();

    public void AssertCanMutate()
    {
        if (Status == GenericDocumentState.Posted || Status == GenericDocumentState.Closed || Status == GenericDocumentState.Cancelled)
            throw new InvalidOperationException($"Cannot modify Sales Order {OrderNumber} in status '{Status}'. Immutability enforced.");
    }
}

public class SalesOrderLine : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesOrderId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal OrderedQty { get; set; }
    public decimal DeliveredQty { get; set; } = 0m;
    public decimal InvoicedQty { get; set; } = 0m;
    public decimal OpenQty => Math.Max(0m, OrderedQty - DeliveredQty);
    public string UomCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public Guid WarehouseId { get; set; }
}

public class SalesDelivery : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeliveryNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid BranchId { get; set; }
    public GenericDocumentState Status { get; set; } = GenericDocumentState.Draft;
    public DateOnly DeliveryDate { get; set; }

    public ICollection<SalesDeliveryLine> Lines { get; set; } = new List<SalesDeliveryLine>();

    public void AssertCanMutate()
    {
        if (Status == GenericDocumentState.Posted || Status == GenericDocumentState.Closed || Status == GenericDocumentState.Cancelled)
            throw new InvalidOperationException($"Cannot modify Sales Delivery {DeliveryNumber} in status '{Status}'. Immutability enforced.");
    }
}

public class SalesDeliveryLine : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeliveryId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public decimal DeliveredQty { get; set; }
    public string UomCode { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? SerialId { get; set; }
    public Guid? SourceOrderLineId { get; set; }
}

public class SalesInvoice : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public GenericDocumentState Status { get; set; } = GenericDocumentState.Draft;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();

    public void AssertCanMutate()
    {
        if (Status == GenericDocumentState.Posted || Status == GenericDocumentState.Cancelled || Status == GenericDocumentState.Reversed)
            throw new InvalidOperationException($"Cannot modify Sales Invoice {InvoiceNumber} in status '{Status}'. Corrections must be processed via reversal.");
    }
}

public class SalesInvoiceLine : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public decimal InvoicedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? DeliveryLineId { get; set; }
}
