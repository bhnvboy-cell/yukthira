using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.MM;

[Authorize]
public class CreateGRNModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/MM/GRN/Create");
    public IActionResult OnPost() => RedirectToPage("/MM/GRN/Create");
}
