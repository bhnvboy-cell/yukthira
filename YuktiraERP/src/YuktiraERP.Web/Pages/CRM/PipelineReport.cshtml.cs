using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.CRM;
public class PipelineReportModel : PageModel
{
    private readonly IRepository<OpportunityEntity, Guid> _repo;
    public PipelineReportModel(IRepository<OpportunityEntity, Guid> repo) { _repo = repo; }
    public List<OpportunityEntity> Opportunities { get; set; } = new();
    public async Task OnGetAsync() { Opportunities = await _repo.GetAllAsync(); }
}
