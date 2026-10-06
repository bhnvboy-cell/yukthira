using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.Admin;

[Authorize(Policy = "AdminOrAbove")]
public class SoxComplianceModel : PageModel
{
    public void OnGet() { }
}
