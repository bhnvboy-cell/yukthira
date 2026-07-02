using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Web.Models;
namespace YuktiraERP.Web.Pages.MM;
public class CreatePRModel : PageModel
{
    [BindProperty] public PurchaseRequisition PR { get; set; } = new();
    public IActionResult OnGet() => Page();
    public IActionResult OnPost() { if (!ModelState.IsValid) return Page(); PurchaseRequisitionStore.Add(PR); return RedirectToPage("/MM/Index"); }
}
