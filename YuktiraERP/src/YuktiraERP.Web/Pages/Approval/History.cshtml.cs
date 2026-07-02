using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.Approval;
public class HistoryModel : PageModel
{
    private readonly IRepository<ApprovalRequestEntity, Guid> _repo;
    public HistoryModel(IRepository<ApprovalRequestEntity, Guid> repo) { _repo = repo; }
    public List<ApprovalRequestEntity> Items { get; set; } = new();
    public async Task OnGetAsync() { Items = (await _repo.GetAllAsync()).Where(a => a.Status != "Pending").ToList(); }
}
