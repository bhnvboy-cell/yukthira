using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.MM;

[Authorize]
public class CreatePOModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/MM/PO/Create");
    public IActionResult OnPost() => RedirectToPage("/MM/PO/Create");
}
