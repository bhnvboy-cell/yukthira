using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR.Appraisal;
public class CreateModel : PageModel
{
    private readonly IRepository<AppraisalEntity, Guid> _repo;
    public CreateModel(IRepository<AppraisalEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public AppraisalEntity Appraisal { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Appraisal); return RedirectToPage("/HR/Index"); }
}
