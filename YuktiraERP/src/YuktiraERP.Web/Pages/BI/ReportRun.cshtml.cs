using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.BI;
public class ReportRunModel : PageModel
{
    private readonly IRepository<BIReportEntity, Guid> _repo;
    public ReportRunModel(IRepository<BIReportEntity, Guid> repo) { _repo = repo; }
    public List<BIReportEntity> Reports { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReportId { get; set; }
    public string? ResultHtml { get; set; }
    public async Task OnGetAsync() { Reports = await _repo.GetAllAsync(); }
    public async Task<IActionResult> OnPostRunAsync()
    {
        Reports = await _repo.GetAllAsync();
        var report = Reports.FirstOrDefault(r => r.ReportId == ReportId);
        if (report != null) ResultHtml = $"<div class='alert alert-success'>Report '{report.ReportName}' generated successfully.</div>";
        return Page();
    }
}
