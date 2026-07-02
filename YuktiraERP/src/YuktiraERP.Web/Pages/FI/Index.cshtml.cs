using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.FI;

public class IndexModel : PageModel
{
    private readonly IRepository<JournalEntryEntity, Guid> _journalRepo;
    private readonly IRepository<FixedAssetEntity, Guid> _assetRepo;

    public List<JournalEntryEntity> JournalEntries { get; set; } = new();
    public List<FixedAssetEntity> FixedAssets { get; set; } = new();

    public IndexModel(
        IRepository<JournalEntryEntity, Guid> journalRepo,
        IRepository<FixedAssetEntity, Guid> assetRepo)
    {
        _journalRepo = journalRepo;
        _assetRepo = assetRepo;
    }

    public async Task OnGetAsync()
    {
        JournalEntries = await _journalRepo.GetAllAsync();
        FixedAssets = await _assetRepo.GetAllAsync();
    }
}
