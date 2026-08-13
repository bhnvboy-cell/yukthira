using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace YuktiraERP.Web.Controllers;

public class HomeController : Controller
{
    [HttpPost]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        var supported = new[] { "en", "hi", "ta", "te", "kn", "ml", "fr", "es" };
        var cultureValue = string.IsNullOrWhiteSpace(culture) ? "en" : culture;
        if (!supported.Contains(cultureValue)) cultureValue = "en";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureValue)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
