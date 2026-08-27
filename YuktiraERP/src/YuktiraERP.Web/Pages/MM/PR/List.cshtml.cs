using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.PR;

[Authorize]
public class ListModel : PageModel
{
    private readonly IRepository<PurchaseRequisitionEntity, Guid> _repo;
    public ListModel(IRepository<PurchaseRequisitionEntity, Guid> repo) { _repo = repo; }
    public List<PurchaseRequisitionEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.GetAllAsync();

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
