using System;
using System.Globalization;
using Microsoft.AspNetCore.Html;

namespace YuktiraERP.Web.Helpers;

public class CurrencyDisplayModel
{
    public string Code { get; set; } = "USD";
    public string Symbol { get; set; } = "$";
    public string Name { get; set; } = "US Dollar";
    public int DecimalPlaces { get; set; } = 2;
    public string SvgIconId { get; set; } = "bi-currency-dollar";
}

public static class CurrencyFormatter
{
    private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
    private static readonly CultureInfo InCulture = new CultureInfo("en-IN");
    private static readonly CultureInfo EuCulture = new CultureInfo("fr-FR");
    private static readonly CultureInfo GbCulture = new CultureInfo("en-GB");

    public static CurrencyDisplayModel GetCurrencyModel(string? currencyCode)
    {
        return (currencyCode?.ToUpperInvariant()) switch
        {
            "INR" => new CurrencyDisplayModel { Code = "INR", Symbol = "₹", Name = "Indian Rupee", SvgIconId = "bi-currency-rupee" },
            "EUR" => new CurrencyDisplayModel { Code = "EUR", Symbol = "€", Name = "Euro", SvgIconId = "bi-currency-euro" },
            "GBP" => new CurrencyDisplayModel { Code = "GBP", Symbol = "£", Name = "British Pound", SvgIconId = "bi-currency-pound" },
            "JPY" => new CurrencyDisplayModel { Code = "JPY", Symbol = "¥", Name = "Japanese Yen", DecimalPlaces = 0, SvgIconId = "bi-currency-yen" },
            _ => new CurrencyDisplayModel { Code = "USD", Symbol = "$", Name = "US Dollar", SvgIconId = "bi-currency-dollar" }
        };
    }

    /// <summary>
    /// Formats decimal amount into clean currency string with guaranteed symbol (no generic currency symbol ¤).
    /// </summary>
    public static string Format(decimal amount, string? currencyCode = "USD")
    {
        var model = GetCurrencyModel(currencyCode);
        var culture = (model.Code) switch
        {
            "INR" => InCulture,
            "EUR" => EuCulture,
            "GBP" => GbCulture,
            _ => UsCulture
        };

        // Format number with decimal places
        string numberPart = amount.ToString($"N{model.DecimalPlaces}", culture);

        // Standard prefix format
        return $"{model.Symbol}{numberPart}";
    }

    /// <summary>
    /// Formats amount into an SVG icon or styled currency badge for high-precision UI.
    /// </summary>
    public static IHtmlContent RenderBadge(decimal amount, string? currencyCode = "USD")
    {
        var model = GetCurrencyModel(currencyCode);
        string formatted = Format(amount, currencyCode);
        string html = $"<span class=\"yuktira-currency-badge\"><svg class=\"currency-svg-icon\" aria-hidden=\"true\"><use href=\"/images/sprite.svg#{model.SvgIconId}\"></use></svg><span class=\"currency-amount\">{formatted}</span></span>";
        return new HtmlString(html);
    }
}
