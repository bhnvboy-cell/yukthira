using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.MultiTenant;

public class TenantCultureMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly Dictionary<string, CultureInfo> CurrencyCultureMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "INR", new CultureInfo("en-IN") },
        { "USD", new CultureInfo("en-US") },
        { "EUR", new CultureInfo("de-DE") },
        { "GBP", new CultureInfo("en-GB") },
        { "JPY", new CultureInfo("ja-JP") },
        { "CNY", new CultureInfo("zh-CN") },
        { "KRW", new CultureInfo("ko-KR") },
        { "AUD", new CultureInfo("en-AU") },
        { "CAD", new CultureInfo("en-CA") },
        { "CHF", new CultureInfo("de-CH") },
        { "SGD", new CultureInfo("en-SG") },
        { "AED", new CultureInfo("ar-AE") },
        { "SAR", new CultureInfo("ar-SA") },
        { "BRL", new CultureInfo("pt-BR") },
        { "ZAR", new CultureInfo("zu-ZA") },
        { "RUB", new CultureInfo("ru-RU") },
        { "MXN", new CultureInfo("es-MX") },
        { "IDR", new CultureInfo("id-ID") },
        { "TRY", new CultureInfo("tr-TR") },
        { "THB", new CultureInfo("th-TH") },
        { "MYR", new CultureInfo("ms-MY") },
        { "PHP", new CultureInfo("fil-PH") },
        { "VND", new CultureInfo("vi-VN") },
        { "PLN", new CultureInfo("pl-PL") },
        { "SEK", new CultureInfo("sv-SE") },
        { "NOK", new CultureInfo("nb-NO") },
        { "DKK", new CultureInfo("da-DK") },
        { "NZD", new CultureInfo("en-NZ") },
        { "HKD", new CultureInfo("zh-HK") },
        { "TWD", new CultureInfo("zh-TW") },
        { "EGP", new CultureInfo("ar-EG") },
        { "NGN", new CultureInfo("en-NG") },
        { "KES", new CultureInfo("en-KE") },
        { "GHS", new CultureInfo("en-GH") },
        { "LKR", new CultureInfo("si-LK") },
        { "BDT", new CultureInfo("bn-BD") },
        { "PKR", new CultureInfo("en-PK") },
        { "PHP", new CultureInfo("fil-PH") },
        { "COP", new CultureInfo("es-CO") },
        { "CLP", new CultureInfo("es-CL") },
        { "PEN", new CultureInfo("es-PE") },
        { "ARS", new CultureInfo("es-AR") },
        { "KWD", new CultureInfo("ar-KW") },
        { "QAR", new CultureInfo("ar-QA") },
        { "BHD", new CultureInfo("ar-BH") },
        { "OMR", new CultureInfo("ar-OM") },
        { "JOD", new CultureInfo("ar-JO") },
        { "LBP", new CultureInfo("ar-LB") },
    };

    public TenantCultureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Items.TryGetValue("TenantId", out var tenantIdObj) &&
            tenantIdObj is Guid tenantId)
        {
            try
            {
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<YuktiraDbContext>();

                var baseCurrency = await db.Set<CurrencyEntity>()
                    .AsNoTracking()
                    .Where(c => c.TenantId == tenantId && c.IsBase && c.IsActive)
                    .Select(c => c.Code)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(baseCurrency))
                {
                    if (CurrencyCultureMap.TryGetValue(baseCurrency, out var culture))
                    {
                        var cloned = (CultureInfo)culture.Clone();
                        cloned.NumberFormat.CurrencySymbol = GetCurrencySymbol(baseCurrency);
                        Thread.CurrentThread.CurrentCulture = cloned;
                        Thread.CurrentThread.CurrentUICulture = cloned;
                    }

                    context.Items["TenantCurrencyCode"] = baseCurrency;
                    context.Items["TenantCurrencySymbol"] = GetCurrencySymbol(baseCurrency);
                }
                else
                {
                    context.Items["TenantCurrencyCode"] = "INR";
                    context.Items["TenantCurrencySymbol"] = "₹";
                }
            }
            catch
            {
                context.Items["TenantCurrencyCode"] = "INR";
                context.Items["TenantCurrencySymbol"] = "₹";
            }
        }

        await _next(context);
    }

    private static string GetCurrencySymbol(string currencyCode)
    {
        return currencyCode.ToUpperInvariant() switch
        {
            "INR" => "₹",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "JPY" => "¥",
            "CNY" => "¥",
            "KRW" => "₩",
            "AUD" => "A$",
            "CAD" => "C$",
            "CHF" => "CHF",
            "SGD" => "S$",
            "AED" => "د.إ",
            "SAR" => "﷼",
            "BRL" => "R$",
            "ZAR" => "R",
            "RUB" => "₽",
            "MXN" => "MX$",
            "IDR" => "Rp",
            "TRY" => "₺",
            "THB" => "฿",
            "MYR" => "RM",
            "PHP" => "₱",
            "VND" => "₫",
            "PLN" => "zł",
            "SEK" => "kr",
            "NOK" => "kr",
            "DKK" => "kr",
            "NZD" => "NZ$",
            "HKD" => "HK$",
            "TWD" => "NT$",
            "EGP" => "E£",
            "NGN" => "₦",
            "KES" => "KSh",
            "GHS" => "GH₵",
            "LKR" => "Rs",
            "BDT" => "৳",
            "PKR" => "Rs",
            "COP" => "COL$",
            "CLP" => "CL$",
            "PEN" => "S/.",
            "ARS" => "AR$",
            "KWD" => "د.ك",
            "QAR" => "﷼",
            "BHD" => ".د.ب",
            "OMR" => "﷼",
            "JOD" => "د.ا",
            "LBP" => "L£",
            _ => currencyCode,
        };
    }
}
