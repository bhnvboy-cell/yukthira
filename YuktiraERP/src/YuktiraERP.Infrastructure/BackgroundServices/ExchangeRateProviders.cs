using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Infrastructure.BackgroundServices;

/// <summary>
/// European Central Bank daily reference rates (EUR based feed).
/// Feed: https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml
/// </summary>
public class EcbExchangeRateProvider : IExchangeRateProvider
{
    public const string DailyRatesUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EcbExchangeRateProvider> _logger;

    public EcbExchangeRateProvider(IHttpClientFactory httpClientFactory, ILogger<EcbExchangeRateProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "ECB";

    public string? LastError { get; private set; }

    public async Task<Dictionary<string, decimal>> GetLatestRatesAsync(string baseCurrency, CancellationToken ct = default)
    {
        LastError = null;
        try
        {
            var client = _httpClientFactory.CreateClient("fx");
            var xml = await client.GetStringAsync(DailyRatesUrl, ct);
            return ParseDailyXml(xml, baseCurrency);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogWarning(ex, "ECB exchange rate fetch failed");
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Parses the ECB daily XML (Cube time / Cube currency-rate series) and re-bases the
    /// EUR quoted rates onto <paramref name="baseCurrency"/> (rate(base -> X) = eurToX / eurToBase).
    /// The base currency itself is returned with rate 1. Never returns the base currency at 0.
    /// </summary>
    public static Dictionary<string, decimal> ParseDailyXml(string xml, string baseCurrency)
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(xml)) return rates;

        var doc = XDocument.Parse(xml);

        var eurRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var cube in doc.Descendants().Where(e => e.Name.LocalName == "Cube"))
        {
            var currency = cube.Attribute("currency")?.Value;
            var rateText = cube.Attribute("rate")?.Value;
            if (string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(rateText)) continue;
            if (!decimal.TryParse(rateText, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate)) continue;
            if (rate <= 0) continue;

            eurRates[currency.Trim().ToUpperInvariant()] = rate;
        }

        if (eurRates.Count == 0) return rates;

        // ECB quotes "units of currency per 1 EUR".
        eurRates["EUR"] = 1m;

        var baseCode = (baseCurrency ?? "EUR").Trim().ToUpperInvariant();
        if (baseCode.Length == 0) baseCode = "EUR";

        if (baseCode == "EUR")
        {
            foreach (var pair in eurRates) rates[pair.Key] = pair.Value;
            return rates;
        }

        if (!eurRates.TryGetValue(baseCode, out var eurToBase) || eurToBase <= 0)
        {
            throw new InvalidOperationException($"The ECB feed does not contain a rate for base currency {baseCode}.");
        }

        rates[baseCode] = 1m;
        foreach (var pair in eurRates)
        {
            if (string.Equals(pair.Key, baseCode, StringComparison.OrdinalIgnoreCase)) continue;
            rates[pair.Key] = pair.Value / eurToBase;
        }

        return rates;
    }
}

/// <summary>
/// Keyless public feed from open.er-api.com (Open Exchange Rates compatible shape):
/// GET https://open.er-api.com/v6/latest/{base} -> { "rates": { "USD": 1, ... } }.
/// </summary>
public class OpenExchangeRatesProvider : IExchangeRateProvider
{
    public const string LatestRatesUrl = "https://open.er-api.com/v6/latest/";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenExchangeRatesProvider> _logger;

    public OpenExchangeRatesProvider(IHttpClientFactory httpClientFactory, ILogger<OpenExchangeRatesProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "OpenExchangeRates";

    public string? LastError { get; private set; }

    public async Task<Dictionary<string, decimal>> GetLatestRatesAsync(string baseCurrency, CancellationToken ct = default)
    {
        LastError = null;
        try
        {
            var baseCode = (baseCurrency ?? "EUR").Trim().ToUpperInvariant();
            if (baseCode.Length == 0) baseCode = "EUR";

            var client = _httpClientFactory.CreateClient("fx");
            var json = await client.GetStringAsync(LatestRatesUrl + baseCode, ct);
            return ParseLatestJson(json, baseCode);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogWarning(ex, "OpenExchangeRates fetch failed");
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Parses the { "rates": { "CODE": number } } payload. Pure helper (no network).</summary>
    public static Dictionary<string, decimal> ParseLatestJson(string json, string baseCurrency)
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return rates;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("rates", out var ratesElement) ||
            ratesElement.ValueKind != JsonValueKind.Object)
        {
            return rates;
        }

        foreach (var property in ratesElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Number) continue;
            if (!property.Value.TryGetDecimal(out var rate)) continue;
            if (rate <= 0) continue;

            rates[property.Name.Trim().ToUpperInvariant()] = rate;
        }

        var baseCode = (baseCurrency ?? "").Trim().ToUpperInvariant();
        if (baseCode.Length > 0 && !rates.ContainsKey(baseCode)) rates[baseCode] = 1m;

        return rates;
    }
}
