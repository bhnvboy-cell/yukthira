using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.Approval;
[Authorize]
public class PendingModel : PageModel
{
    private readonly IRepository<ApprovalRequestEntity, Guid> _repo;
    public PendingModel(IRepository<ApprovalRequestEntity, Guid> repo) { _repo = repo; }
    public List<ApprovalRequestEntity> Items { get; set; } = new();
    public async Task OnGetAsync() { Items = (await _repo.GetAllAsync()).Where(a => a.Status == "Pending").ToList(); }
}
