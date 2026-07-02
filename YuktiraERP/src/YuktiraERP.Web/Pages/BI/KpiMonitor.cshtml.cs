using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.BI;
public class KpiMonitorModel : PageModel
{
    public Dictionary<string, object> Kpis { get; set; } = new();
    public void OnGet()
    {
        Kpis["Total Revenue"] = 1250000m;
        Kpis["Open Orders"] = 47;
        Kpis["Pending Approvals"] = 12;
        Kpis["Stock Coverage Days"] = 34;
        Kpis["On-Time Delivery %"] = 92.5;
        Kpis["Employee Headcount"] = 156;
    }
}
