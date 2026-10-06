namespace YuktiraERP.Core.Dtos;

/// <summary>One row of the tenant exchange rate matrix.</summary>
public class ExchangeRateMatrixRowDto
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public decimal Rate { get; set; }
    public decimal Inverse { get; set; }
    public string Source { get; set; } = "";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Manual (SOX controlled) rate override request. Comment is mandatory.</summary>
public class ManualRateOverrideRequest
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public decimal Rate { get; set; }
    public string Comment { get; set; } = "";
}

public class ExchangeRateConvertRequest
{
    public decimal Amount { get; set; }
    public string From { get; set; } = "";
    public string To { get; set; } = "";
}

public class ExchangeRateConvertResultDto
{
    public decimal Amount { get; set; }
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public decimal Rate { get; set; }
    public decimal Converted { get; set; }
    public DateTime AsOf { get; set; }
}

/// <summary>
/// Pure rate resolution math used by the convert endpoint (kept out of CurrencyService,
/// which is owned by another task) and covered by unit tests.
/// </summary>
public static class ExchangeRateMath
{
    /// <summary>
    /// Resolves the rate from <paramref name="from"/> to <paramref name="to"/>:
    /// exact pair first, then the inverse pair, then a cross through any common base currency
    /// (rate(base -&gt; to) / rate(base -&gt; from)).
    /// <paramref name="rates"/> keys are expected to be upper case currency codes.
    /// </summary>
    public static bool TryResolveRate(
        IReadOnlyDictionary<(string From, string To), decimal> rates,
        string? from,
        string? to,
        out decimal rate)
    {
        rate = 1m;
        if (rates == null) return false;

        var fromCode = (from ?? "").Trim().ToUpperInvariant();
        var toCode = (to ?? "").Trim().ToUpperInvariant();
        if (fromCode.Length == 0 || toCode.Length == 0) return false;
        if (fromCode == toCode) return true;

        if (rates.TryGetValue((fromCode, toCode), out var direct) && direct > 0)
        {
            rate = direct;
            return true;
        }

        if (rates.TryGetValue((toCode, fromCode), out var inverse) && inverse > 0)
        {
            rate = 1m / inverse;
            return true;
        }

        foreach (var baseCode in CollectBaseCandidates(rates))
        {
            if (baseCode == fromCode || baseCode == toCode) continue;
            if (!TryLeg(rates, baseCode, fromCode, out var fromLeg) || fromLeg <= 0) continue;
            if (!TryLeg(rates, baseCode, toCode, out var toLeg) || toLeg <= 0) continue;

            rate = toLeg / fromLeg;
            return true;
        }

        return false;
    }

    /// <summary>1 / rate rounded to 6 decimals for display. Zero or negative rates return 0.</summary>
    public static decimal InverseRate(decimal rate)
    {
        if (rate <= 0) return 0m;
        return Math.Round(1m / rate, 6, MidpointRounding.AwayFromZero);
    }

    private static bool TryLeg(
        IReadOnlyDictionary<(string From, string To), decimal> rates,
        string baseCode,
        string code,
        out decimal value)
    {
        if (rates.TryGetValue((baseCode, code), out var direct) && direct > 0)
        {
            value = direct;
            return true;
        }

        if (rates.TryGetValue((code, baseCode), out var inverse) && inverse > 0)
        {
            value = 1m / inverse;
            return true;
        }

        value = 0m;
        return false;
    }

    private static List<string> CollectBaseCandidates(IReadOnlyDictionary<(string From, string To), decimal> rates)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>();
        foreach (var key in rates.Keys)
        {
            if (!string.IsNullOrWhiteSpace(key.From) && seen.Add(key.From)) candidates.Add(key.From);
            if (!string.IsNullOrWhiteSpace(key.To) && seen.Add(key.To)) candidates.Add(key.To);
        }
        return candidates;
    }
}
