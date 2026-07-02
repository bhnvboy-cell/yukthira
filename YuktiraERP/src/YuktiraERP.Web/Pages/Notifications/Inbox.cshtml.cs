using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.Notifications;
public class InboxModel : PageModel
{
    private readonly IRepository<NotificationEntity, Guid> _repo;
    public InboxModel(IRepository<NotificationEntity, Guid> repo) { _repo = repo; }
    public List<NotificationEntity> Notifications { get; set; } = new();
    public async Task OnGetAsync() { Notifications = await _repo.GetAllAsync(); }
}
