using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.Batch;

[Authorize]
public class ListModel : PageModel
{
    private readonly IRepository<BatchEntity, Guid> _batchRepo;
    public List<BatchEntity> Batches { get; set; } = new();

    public ListModel(IRepository<BatchEntity, Guid> batchRepo) => _batchRepo = batchRepo;

    public async Task OnGetAsync()
    {
        Batches = await _batchRepo.GetAllAsync();
    }
}
