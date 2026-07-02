using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.SD.Inquiry;
public class CreateModel : PageModel
{
    private readonly IRepository<InquiryEntity, Guid> _repo;
    public CreateModel(IRepository<InquiryEntity, Guid> repo) { _repo = repo; }
    [BindProperty] public InquiryEntity Inquiry { get; set; } = new();
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); await _repo.AddAsync(Inquiry); return RedirectToPage("/SD/Index"); }
}
