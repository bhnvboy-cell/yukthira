using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Web.Models;
namespace YuktiraERP.Web.Pages.MM;
public class CreateVendorModel : PageModel
{
    [BindProperty] public Models.Vendor Vendor { get; set; } = new();
    public IActionResult OnGet() => Page();
    public IActionResult OnPost() { if (!ModelState.IsValid) return Page(); VendorStore.Add(Vendor); return RedirectToPage("/MM/Index"); }
}
