using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI.AR;
public class PaymentModel : PageModel
{
    [BindProperty] public string CustomerName { get; set; } = "";
    [BindProperty] public decimal Amount { get; set; }
    [BindProperty] public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string? Message { get; set; }
    public void OnGet() { }
    public IActionResult OnPost()
    {
        Message = $"Payment of {Amount:C2} to {CustomerName} on {PaymentDate:d} recorded.";
        return Page();
    }
}
