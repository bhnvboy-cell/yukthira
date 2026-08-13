using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.FI;

public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/FI/Ledger/Create");
    public IActionResult OnPost() => RedirectToPage("/FI/Ledger/Create");
}
