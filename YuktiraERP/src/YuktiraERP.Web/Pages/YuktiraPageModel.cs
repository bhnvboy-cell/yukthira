using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages;

public abstract class YuktiraPageModel : PageModel
{
    public string ActiveCurrencyCode => HttpContext?.Items["TenantCurrencyCode"]?.ToString() ?? "INR";
    public string ActiveCurrencySymbol => HttpContext?.Items["TenantCurrencySymbol"]?.ToString() ?? "₹";
    public string ActiveLocale
    {
        get
        {
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            return $"{culture.TwoLetterISOLanguageName}-{culture.Name.Split('-').Last()}";
        }
    }
    public string TenantId => HttpContext?.Items["TenantId"]?.ToString() ?? "";

    protected string FormatCurrency(decimal amount)
    {
        return amount.ToString("C", System.Globalization.CultureInfo.CurrentCulture);
    }
}
