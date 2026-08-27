using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.QM;

[Authorize]
public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/QM/InspectionLot/Create");
    public IActionResult OnPost() => RedirectToPage("/QM/InspectionLot/Create");
}
