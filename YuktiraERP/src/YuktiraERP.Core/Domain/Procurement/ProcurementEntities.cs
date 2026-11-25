using System;
using System.Collections.Generic;
using YuktiraERP.Core.Domain.Audit;
using YuktiraERP.Core.Domain.Common;

namespace YuktiraERP.Core.Domain.Procurement;

public class PurchaseOrder : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PoNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public Guid BranchId { get; set; }
    public GenericDocumentState Status { get; set; } = GenericDocumentState.Draft;
    public DateOnly DocumentDate { get; set; }
    public DateOnly ExpectedDeliveryDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal SubTotal { get; set; } = 0m;
    public decimal TaxAmount { get; set; } = 0m;
    public decimal TotalAmount { get; set; } = 0m;

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();

    public void AssertCanMutate()
    {
        if (Status == GenericDocumentState.Posted || Status == GenericDocumentState.Closed || Status == GenericDocumentState.Cancelled)
            throw new InvalidOperationException($"Cannot modify Purchase Order {PoNumber} in status '{Status}'. Immutability enforced.");
    }
}

public class PurchaseOrderLine : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseOrderId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal OrderedQty { get; set; }
    public decimal ReceivedQty { get; set; } = 0m;
    public decimal InvoicedQty { get; set; } = 0m;
    public decimal OpenQty => Math.Max(0m, OrderedQty - ReceivedQty);
    public string UomCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public Guid WarehouseId { get; set; }
}

public class GoodsReceiptPo : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string GrpoNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public Guid BranchId { get; set; }
    public GenericDocumentState Status { get; set; } = GenericDocumentState.Draft;
    public DateOnly ReceiptDate { get; set; }
    public string? VendorDeliveryNote { get; set; }

    public ICollection<GoodsReceiptPoLine> Lines { get; set; } = new List<GoodsReceiptPoLine>();

    public void AssertCanMutate()
    {
        if (Status == GenericDocumentState.Posted || Status == GenericDocumentState.Closed || Status == GenericDocumentState.Cancelled)
            throw new InvalidOperationException($"Cannot modify Goods Receipt PO {GrpoNumber} in status '{Status}'. Immutability enforced.");
    }
}

public class GoodsReceiptPoLine : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GoodsReceiptPoId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public decimal ReceivedQty { get; set; }
    public string UomCode { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? SerialId { get; set; }
    public Guid? SourcePoLineId { get; set; }
}

public class ApInvoice : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string VendorInvoiceReference { get; set; } = string.Empty;
    public GenericDocumentState Status { get; set; } = GenericDocumentState.Draft;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<ApInvoiceLine> Lines { get; set; } = new List<ApInvoiceLine>();

    public void AssertCanMutate()
    {
        if (Status == GenericDocumentState.Posted || Status == GenericDocumentState.Cancelled || Status == GenericDocumentState.Reversed)
            throw new InvalidOperationException($"Cannot modify AP Invoice {InvoiceNumber} in status '{Status}'. Corrections must be processed via reversal.");
    }
}

public class ApInvoiceLine : BaseAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApInvoiceId { get; set; }
    public int LineNumber { get; set; }
    public Guid ItemId { get; set; }
    public decimal InvoicedQty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? GrpoLineId { get; set; }
    public Guid? PoLineId { get; set; }
}
