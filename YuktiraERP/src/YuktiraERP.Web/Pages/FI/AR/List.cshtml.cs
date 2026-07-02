using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI.AR;
public class ListModel : PageModel
{
    private readonly IRepository<AREntryEntity, Guid> _repo;
    public ListModel(IRepository<AREntryEntity, Guid> repo) { _repo = repo; }
    public List<AREntryEntity> Entries { get; set; } = new();
    public async Task OnGetAsync() { Entries = await _repo.GetAllAsync(); }
}
