using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.QM.COA;

public class DisplayModel : PageModel
{
    private readonly IRepository<CertificateOfAnalysisEntity, Guid> _repo;
    public CertificateOfAnalysisEntity Certificate { get; set; } = new();
    public DisplayModel(IRepository<CertificateOfAnalysisEntity, Guid> repo) => _repo = repo;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return NotFound();
        Certificate = entity;
        return Page();
    }
}
