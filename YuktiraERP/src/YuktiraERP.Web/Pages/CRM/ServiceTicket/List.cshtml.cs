using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.CRM.ServiceTicket;

[Authorize]
public class ListModel : PageModel
{
    private readonly IRepository<ServiceTicketEntity, Guid> _repo;
    public ListModel(IRepository<ServiceTicketEntity, Guid> repo) { _repo = repo; }
    public List<ServiceTicketEntity> Items { get; set; } = new();

    public async Task OnGetAsync() => Items = await _repo.GetAllAsync();

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _repo.DeleteAsync(id);
        return RedirectToPage();
    }
}
