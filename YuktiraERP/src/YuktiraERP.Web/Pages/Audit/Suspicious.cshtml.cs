using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.Audit;
[Authorize(Policy = "AdminOrAbove")]
public class SuspiciousModel : PageModel
{
    private readonly IRepository<AuditLogEntity, Guid> _repo;
    public SuspiciousModel(IRepository<AuditLogEntity, Guid> repo) { _repo = repo; }
    public List<AuditLogEntity> Entries { get; set; } = new();
    public async Task OnGetAsync() { Entries = (await _repo.GetAllAsync()).Where(e => e.IsFlagged).ToList(); }
}
