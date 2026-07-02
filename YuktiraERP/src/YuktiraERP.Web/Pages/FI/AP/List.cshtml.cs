using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI.AP;
public class ListModel : PageModel
{
    private readonly IRepository<APEntryEntity, Guid> _repo;
    public ListModel(IRepository<APEntryEntity, Guid> repo) { _repo = repo; }
    public List<APEntryEntity> Entries { get; set; } = new();
    public async Task OnGetAsync() { Entries = await _repo.GetAllAsync(); }
}
