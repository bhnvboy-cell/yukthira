using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
using YuktiraERP.Web.Models;
namespace YuktiraERP.Web.Pages.Approval;
public class CreateModel : PageModel
{
    private readonly IRepository<ApprovalRequestEntity, Guid> _repo;
    public CreateModel(IRepository<ApprovalRequestEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public ApprovalRequest ApprovalRequest { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _repo.AddAsync(new ApprovalRequestEntity
        {
            RequestId = ApprovalRequest.RequestId,
            Type = ApprovalRequest.Type,
            Subject = ApprovalRequest.Subject,
            Requestor = ApprovalRequest.Requestor,
            RequestDate = ApprovalRequest.RequestDate,
            Amount = ApprovalRequest.Amount,
            Status = ApprovalRequest.Status
        });
        return RedirectToPage("/Approval/Index");
    }
}
