using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.LIMS.Specification;
public class CreateModel : PageModel
{
    private readonly IRepository<SpecificationEntity, Guid> _repo;
    public CreateModel(IRepository<SpecificationEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public SpecificationEntity Spec { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Spec); return RedirectToPage("/LIMS/Index"); }
}
