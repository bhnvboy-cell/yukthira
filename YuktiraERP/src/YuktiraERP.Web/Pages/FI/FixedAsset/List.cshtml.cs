using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.FI.FixedAsset;

[Authorize]
public class ListModel : PageModel
{
    private readonly IRepository<FixedAssetEntity, Guid> _repo;
    public ListModel(IRepository<FixedAssetEntity, Guid> repo) { _repo = repo; }
    public List<FixedAssetEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.GetAllAsync();

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
