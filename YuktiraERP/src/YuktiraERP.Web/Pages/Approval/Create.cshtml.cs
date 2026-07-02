using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Web.Models;
namespace YuktiraERP.Web.Pages.Approval;
public class CreateModel : PageModel
{
    [BindProperty] public ApprovalRequest ApprovalRequest { get; set; } = new();
    public IActionResult OnGet() => Page();
    public IActionResult OnPost() { if (!ModelState.IsValid) return Page(); ApprovalRequestStore.Add(ApprovalRequest); return RedirectToPage("/Approval/Index"); }
}
