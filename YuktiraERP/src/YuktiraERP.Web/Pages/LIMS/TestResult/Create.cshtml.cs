using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.LIMS.TestResult;
public class CreateModel : PageModel
{
    private readonly IRepository<TestResultEntity, Guid> _repo;
    public CreateModel(IRepository<TestResultEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public TestResultEntity Result { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Result); return RedirectToPage("/LIMS/Index"); }
}
