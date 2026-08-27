using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.PP;

[Authorize]
public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/PP/Plan/Create");
    public IActionResult OnPost() => RedirectToPage("/PP/Plan/Create");
}
