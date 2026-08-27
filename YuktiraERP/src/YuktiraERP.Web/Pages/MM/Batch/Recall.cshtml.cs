using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Infrastructure.Caching;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.Batch;

[Authorize]
public class RecallModel : PageModel
{
    private readonly IBatchService _batchService;

    public RecallModel(IBatchService batchService) => _batchService = batchService;

    public List<RecallEntity> Recalls { get; set; } = new();

    public async Task OnGetAsync()
    {
        Recalls = await _batchService.GetRecallsAsync();
    }
}
