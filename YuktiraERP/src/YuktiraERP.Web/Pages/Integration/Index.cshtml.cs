using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.Integration;

[Authorize(Roles = "SUPER_USER,ADMIN")]
public class IndexModel : PageModel
{
    public void OnGet() { }
}
