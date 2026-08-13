using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.HR;

public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/HR/Employee/Create");
    public IActionResult OnPost() => RedirectToPage("/HR/Employee/Create");
}
