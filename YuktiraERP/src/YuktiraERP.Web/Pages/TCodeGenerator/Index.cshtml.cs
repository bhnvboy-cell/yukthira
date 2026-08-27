using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace YuktiraERP.Web.Pages.TCodeGenerator;

[Authorize(Policy = "AdminOrAbove")]
public class IndexModel : PageModel
{
    public void OnGet() { }
}
