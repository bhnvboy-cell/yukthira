using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CO.ProfitCenter;

public class ListModel : PageModel
{
    private readonly IRepository<ProfitCenterEntity, Guid> _repo;
    public ListModel(IRepository<ProfitCenterEntity, Guid> repo) { _repo = repo; }
    public List<ProfitCenterEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.GetAllAsync();

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
