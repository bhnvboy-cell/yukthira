using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.CRM;

[Authorize]
public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/CRM/Lead/Create");
    public IActionResult OnPost() => RedirectToPage("/CRM/Lead/Create");
}
