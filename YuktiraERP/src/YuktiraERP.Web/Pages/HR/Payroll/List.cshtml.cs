using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR.Payroll;

[Authorize]
public class ListModel : PageModel
{
    private readonly IRepository<PayrollEntryEntity, Guid> _repo;
    public ListModel(IRepository<PayrollEntryEntity, Guid> repo) { _repo = repo; }
    public List<PayrollEntryEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.GetAllAsync();

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
