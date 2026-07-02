using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace YuktiraERP.Web.Pages.FI.Reports;
public class BalanceSheetModel : PageModel
{
    public string Period { get; set; } = DateTime.Now.ToString("yyyy-MM");
    public void OnGet() { }
}
