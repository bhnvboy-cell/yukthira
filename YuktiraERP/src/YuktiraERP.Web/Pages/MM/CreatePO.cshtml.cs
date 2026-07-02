using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Web.Models;
namespace YuktiraERP.Web.Pages.MM;
public class CreatePOModel : PageModel
{
    [BindProperty] public PurchaseOrder PO { get; set; } = new();
    public IActionResult OnGet() => Page();
    public IActionResult OnPost() { if (!ModelState.IsValid) return Page(); PurchaseOrderStore.Add(PO); return RedirectToPage("/MM/Index"); }
}
