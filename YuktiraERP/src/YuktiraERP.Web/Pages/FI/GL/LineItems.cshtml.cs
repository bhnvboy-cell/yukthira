using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.FI.GL;
public class LineItemsModel : PageModel
{
    private readonly IRepository<GeneralLedgerEntryEntity, Guid> _repo;
    public LineItemsModel(IRepository<GeneralLedgerEntryEntity, Guid> repo) { _repo = repo; }
    public List<GeneralLedgerEntryEntity> Entries { get; set; } = new();
    public async Task OnGetAsync() { Entries = await _repo.GetAllAsync(); }
}
