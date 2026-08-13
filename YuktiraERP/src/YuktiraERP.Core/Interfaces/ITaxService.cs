namespace YuktiraERP.Core.Interfaces;

public class TaxCodeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }
    public string TaxType { get; set; } = "GST";
    public string TaxAccountCode { get; set; } = "2300";
    public bool IsCompound { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TaxLineRequest
{
    public string TaxCode { get; set; } = "";
    public decimal NetAmount { get; set; }
}

public class TaxCalculationRequest
{
    public string DocumentNumber { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public string DocumentType { get; set; } = "AR";
    public string PartyName { get; set; } = "";
    public List<TaxLineRequest> Lines { get; set; } = new();
}

public class TaxLineResult
{
    public string TaxCode { get; set; } = "";
    public string TaxName { get; set; } = "";
    public decimal Rate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
}

public class TaxBreakdownDto
{
    public string DocumentNumber { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string PartyName { get; set; } = "";
    public decimal TotalNet { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalGross { get; set; }
    public List<TaxLineResult> Lines { get; set; } = new();
}

public interface ITaxService
{
    Task<List<TaxCodeDto>> GetTaxCodesAsync(Guid tenantId);
    Task<TaxCodeDto> CreateTaxCodeAsync(Guid tenantId, TaxCodeDto request);
    Task<TaxCodeDto?> UpdateTaxCodeAsync(Guid tenantId, Guid id, TaxCodeDto request);
    Task DeleteTaxCodeAsync(Guid tenantId, Guid id);
    Task<TaxBreakdownDto> CalculateAsync(Guid tenantId, TaxCalculationRequest request);
    Task<TaxBreakdownDto> PostInvoiceAsync(Guid tenantId, TaxCalculationRequest request);
    Task<List<TaxTransactionDto>> GetTaxTransactionsAsync(Guid tenantId, int limit = 100);
}

public class TaxTransactionDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string TaxCode { get; set; } = "";
    public string TaxName { get; set; } = "";
    public decimal Rate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = "";
}