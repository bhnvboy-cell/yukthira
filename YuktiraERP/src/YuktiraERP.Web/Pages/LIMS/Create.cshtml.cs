using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.LIMS;

public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/LIMS/Sample/Create");
    public IActionResult OnPost() => RedirectToPage("/LIMS/Sample/Create");
}
