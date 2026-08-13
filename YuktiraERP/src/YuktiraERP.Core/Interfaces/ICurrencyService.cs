namespace YuktiraERP.Core.Interfaces;

public class CurrencyDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public bool IsBase { get; set; }
    public int DecimalPlaces { get; set; } = 2;
    public bool IsActive { get; set; } = true;
}

public class ExchangeRateDto
{
    public Guid Id { get; set; }
    public string FromCurrency { get; set; } = "";
    public string ToCurrency { get; set; } = "";
    public decimal Rate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Source { get; set; } = "Manual";
}

public class CurrencyConversionRequest
{
    public decimal Amount { get; set; }
    public string FromCurrency { get; set; } = "";
    public string ToCurrency { get; set; } = "";
    public DateTime? AsOf { get; set; }
}

public class CurrencyConversionResult
{
    public decimal Amount { get; set; }
    public string FromCurrency { get; set; } = "";
    public string ToCurrency { get; set; } = "";
    public decimal Rate { get; set; }
    public decimal ConvertedAmount { get; set; }
}

public class CurrencyRevaluationRequest
{
    public string ToCurrency { get; set; } = "";
    public DateTime? AsOf { get; set; }
}

public interface ICurrencyService
{
    Task<List<CurrencyDto>> GetCurrenciesAsync(Guid tenantId);
    Task<CurrencyDto> CreateCurrencyAsync(Guid tenantId, CurrencyDto request);
    Task<CurrencyDto?> UpdateCurrencyAsync(Guid tenantId, Guid id, CurrencyDto request);
    Task DeleteCurrencyAsync(Guid tenantId, Guid id);
    Task<CurrencyDto?> GetBaseCurrencyAsync(Guid tenantId);

    Task<List<ExchangeRateDto>> GetExchangeRatesAsync(Guid tenantId, string? from = null, string? to = null);
    Task<ExchangeRateDto> SetExchangeRateAsync(Guid tenantId, ExchangeRateDto request);
    Task<CurrencyConversionResult> ConvertAsync(Guid tenantId, CurrencyConversionRequest request);
    Task<decimal> GetRateAsync(Guid tenantId, string from, string to, DateTime? asOf = null);

    Task<List<CurrencyConversionResult>> RevaluateAsync(Guid tenantId, CurrencyRevaluationRequest request);
}