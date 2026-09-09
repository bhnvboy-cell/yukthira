namespace YuktiraERP.Core.Dtos;

public enum PricingConditionType
{
    BasePrice = 0,
    Discount = 1,
    Surcharge = 2,
    Freight = 3,
    Tax = 4,
    Deduction = 5
}

public enum PricingStepStatus
{
    Active = 0,
    Inactive = 1
}

public class PricingContext
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string SalesOrderNumber { get; set; } = "";
    public string DeliveryNumber { get; set; } = ""
    ;
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal BaseUnitPrice { get; set; }
    public string Plant { get; set; } = "";
    public string StorageLocation { get; set; } = "";
    public string Currency { get; set; } = "INR";
    public DateTime PricingDate { get; set; } = DateTime.UtcNow;
    public string TaxCode { get; set; } = "";
    public List<PricingContextLine> Lines { get; set; } = new();
}

public class PricingContextLine
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
}

public class PricingStepInput
{
    public Guid ConditionId { get; set; }
    public int SequenceNumber { get; set; }
    public string ConditionType { get; set; } = "";
    public string ConditionName { get; set; } = "";
    public PricingConditionType Category { get; set; }
    public string CalculationType { get; set; } = "Fixed";
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal PerUnit { get; set; } = 1;
    public string Currency { get; set; } = "INR";
    public bool IsPercentage { get; set; }
    public decimal? PercentageValue { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PricingLineResult
{
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "";
    public List<PricingStepOutput> Steps { get; set; } = new();
    public decimal BaseAmount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalSurcharge { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
}

public class PricingStepOutput
{
    public int SequenceNumber { get; set; }
    public string ConditionType { get; set; } = "";
    public string ConditionName { get; set; } = "";
    public PricingConditionType Category { get; set; }
    public decimal Rate { get; set; }
    public decimal Quantity { get; set; }
    public decimal SubTotal { get; set; }
    public decimal StepAmount { get; set; }
    public bool IsPercentage { get; set; }
    public string Currency { get; set; } = "INR";
}

public class PricingResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Currency { get; set; } = "INR";
    public PricingLineResult? LineResult { get; set; }
    public decimal TotalBaseAmount { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TotalFreightAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal TotalGrossAmount { get; set; }
}

public class PricingConditionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ConditionType { get; set; } = "";
    public string Name { get; set; } = "";
    public PricingConditionType Category { get; set; }
    public string CalculationType { get; set; } = "Fixed";
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal PerUnit { get; set; } = 1;
    public string Currency { get; set; } = "INR";
    public bool IsPercentage { get; set; }
    public decimal? PercentageValue { get; set; }
    public int SequenceNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string ValidFrom { get; set; } = "";
    public string ValidTo { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string MaterialGroup { get; set; } = "";
    public string Plant { get; set; } = "";
    public string Description { get; set; } = "";
}

public class PricingConditionStepDto
{
    public Guid Id { get; set; }
    public Guid ConditionId { get; set; }
    public int SequenceNumber { get; set; }
    public string ConditionType { get; set; } = "";
    public string ConditionName { get; set; } = "";
    public PricingConditionType Category { get; set; }
    public string CalculationType { get; set; } = "Fixed";
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal PerUnit { get; set; } = 1;
    public string Currency { get; set; } = "INR";
    public bool IsPercentage { get; set; }
    public decimal? PercentageValue { get; set; }
    public bool IsActive { get; set; } = true;
    public string Description { get; set; } = "";
}

public class BillingReleaseResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string BillingDocumentNumber { get; set; } = "";
    public string FiDocumentNumber { get; set; } = "";
    public Guid? FiDocumentId { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public List<BillingReleaseJournalLine> JournalLines { get; set; } = new();
    public string SalesOrderId { get; set; } = "";
    public string DeliveryNoteId { get; set; } = "";
    public string FiJournalId { get; set; } = "";
    public DateTime PostedAt { get; set; }
}

public class BillingReleaseJournalLine
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string CostCenter { get; set; } = "";
    public string ProfitCenter { get; set; } = "";
}

public class BillingDocumentDetailDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = "";
    public DateTime Date { get; set; }
    public string SoNumber { get; set; } = "";
    public string DeliveryNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string FiDocumentNumber { get; set; } = "";
    public DateTime? PostedAt { get; set; }
    public List<BillingDocumentLineDto> Lines { get; set; } = new();
    public PricingResult? PricingResult { get; set; }
}

public class BillingDocumentLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal NetAmount { get; set; }
}
