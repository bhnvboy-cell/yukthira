using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace YuktiraERP.Web.Pages.FI.Reports;
[Authorize]
public class BalanceSheetModel : PageModel
{
    public string Period { get; set; } = DateTime.Now.ToString("yyyy-MM");
    public void OnGet() { }
}
