using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI.AP;
public class PaymentModel : PageModel
{
    [BindProperty] public string VendorName { get; set; } = "";
    [BindProperty] public decimal Amount { get; set; }
    [BindProperty] public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string? Message { get; set; }
    public void OnGet() { }
    public IActionResult OnPost()
    {
        Message = $"Payment of {Amount:C2} to {VendorName} on {PaymentDate:d} recorded.";
        return Page();
    }
}
