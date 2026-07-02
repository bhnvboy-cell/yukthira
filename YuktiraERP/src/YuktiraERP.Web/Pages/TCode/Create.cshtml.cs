using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace YuktiraERP.Web.Pages.TCode;

public class CreateModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string TCodeId { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string Code { get; set; } = "";

    public void OnGet() { }
}
