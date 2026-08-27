using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.HR;

[Authorize]
public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/HR/Employee/Create");
    public IActionResult OnPost() => RedirectToPage("/HR/Employee/Create");
}
