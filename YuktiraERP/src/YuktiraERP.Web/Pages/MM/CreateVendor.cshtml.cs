using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.MM;

public class CreateVendorModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/MM/Vendor/Create");
    public IActionResult OnPost() => RedirectToPage("/MM/Vendor/Create");
}
