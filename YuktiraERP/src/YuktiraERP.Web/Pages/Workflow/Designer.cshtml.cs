using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.Workflow;

[Authorize(Policy = "PowerUserOrAbove")]
public class DesignerModel : PageModel
{
    public void OnGet() { }
}
