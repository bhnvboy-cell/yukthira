using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Web.Models;
namespace YuktiraERP.Web.Pages.Approval;
public class IndexModel : PageModel
{
    public IActionResult OnGet() => Page();
    public IActionResult OnPostApprove(string id) { ApprovalRequestStore.Approve(id); return RedirectToPage(); }
    public IActionResult OnPostReject(string id) { ApprovalRequestStore.Reject(id); return RedirectToPage(); }
}
