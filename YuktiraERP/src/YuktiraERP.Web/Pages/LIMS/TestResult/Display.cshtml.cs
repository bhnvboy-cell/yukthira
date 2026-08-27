using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.LIMS.TestResult;

[Authorize]
public class DisplayModel : PageModel
{
    private readonly IRepository<TestResultEntity, Guid> _repo;
    public DisplayModel(IRepository<TestResultEntity, Guid> repo) { _repo = repo; }
    public TestResultEntity Result { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id == null || id == Guid.Empty) return RedirectToPage("/LIMS/TestResult/List");
        var entity = await _repo.GetByIdAsync(id.Value);
        if (entity == null) return NotFound();
        Result = entity;
        return Page();
    }
}
