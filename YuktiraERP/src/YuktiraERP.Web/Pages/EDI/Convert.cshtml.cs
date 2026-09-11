using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace YuktiraERP.Web.Pages.EDI;

[Authorize]
public class ConvertModel : PageModel
{
    public void OnGet() { }
}
