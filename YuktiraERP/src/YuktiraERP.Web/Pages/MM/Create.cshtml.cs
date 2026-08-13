using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.MM;

public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/MM/Material/Create");
    public IActionResult OnPost() => RedirectToPage("/MM/Material/Create");
}
