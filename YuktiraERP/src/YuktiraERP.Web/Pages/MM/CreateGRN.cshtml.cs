using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Web.Models;
namespace YuktiraERP.Web.Pages.MM;
public class CreateGRNModel : PageModel
{
    [BindProperty] public GoodsReceipt GRN { get; set; } = new();
    public IActionResult OnGet() => Page();
    public IActionResult OnPost() { if (!ModelState.IsValid) return Page(); GoodsReceiptStore.Add(GRN); return RedirectToPage("/MM/Index"); }
}
