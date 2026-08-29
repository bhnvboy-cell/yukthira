using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.QM.COA;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IRepository<CertificateOfAnalysisEntity, Guid> _repo;
    public CreateModel(IRepository<CertificateOfAnalysisEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public CertificateOfAnalysisEntity COA { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(COA); return RedirectToPage("/QM/Index"); }
}
