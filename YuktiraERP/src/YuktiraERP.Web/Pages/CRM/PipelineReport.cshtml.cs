using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CRM;
[Authorize]
public class PipelineReportModel : PageModel
{
    private readonly IRepository<OpportunityEntity, Guid> _repo;
    public PipelineReportModel(IRepository<OpportunityEntity, Guid> repo) { _repo = repo; }
    public List<OpportunityEntity> Opportunities { get; set; } = new();
    public async Task OnGetAsync() { Opportunities = await _repo.GetAllAsync(); }
}
