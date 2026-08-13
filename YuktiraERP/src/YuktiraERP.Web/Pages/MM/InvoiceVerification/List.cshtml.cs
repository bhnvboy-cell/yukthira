using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.InvoiceVerification;

public class ListModel : PageModel
{
    private readonly IRepository<InvoiceVerificationEntity, Guid> _repo;
    public ListModel(IRepository<InvoiceVerificationEntity, Guid> repo) { _repo = repo; }
    public List<InvoiceVerificationEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.GetAllAsync();

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
