using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.MM.SerialNumber;

[Authorize]
public class ListModel : PageModel
{
    private readonly IRepository<SerialNumberEntity, Guid> _serialRepo;
    public List<SerialNumberEntity> SerialNumbers { get; set; } = new();

    public ListModel(IRepository<SerialNumberEntity, Guid> serialRepo) => _serialRepo = serialRepo;

    public async Task OnGetAsync()
    {
        SerialNumbers = await _serialRepo.GetAllAsync();
    }
}
