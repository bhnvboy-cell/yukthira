using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.BI.Report;
public class CreateModel : PageModel
{
    private readonly IRepository<BIReportEntity, Guid> _repo;
    public CreateModel(IRepository<BIReportEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public BIReportEntity Report { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Report); return RedirectToPage("/BI/Index"); }
}
