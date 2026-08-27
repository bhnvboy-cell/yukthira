using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.MM;

[Authorize]
public class CreatePRModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/MM/PR/Create");
    public IActionResult OnPost() => RedirectToPage("/MM/PR/Create");
}
