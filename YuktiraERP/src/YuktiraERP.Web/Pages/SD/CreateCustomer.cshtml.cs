using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.SD;

[Authorize]
public class CreateCustomerModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/SD/Customer/Create");
    public IActionResult OnPost() => RedirectToPage("/SD/Customer/Create");
}
