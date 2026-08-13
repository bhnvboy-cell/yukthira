using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.MM;

public class CreatePOModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/MM/PO/Create");
    public IActionResult OnPost() => RedirectToPage("/MM/PO/Create");
}
