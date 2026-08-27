using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CO.InternalOrder;

[Authorize]
public class ListModel : PageModel
{
    private readonly IRepository<InternalOrderEntity, Guid> _repo;
    public ListModel(IRepository<InternalOrderEntity, Guid> repo) { _repo = repo; }
    public List<InternalOrderEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.GetAllAsync();

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
