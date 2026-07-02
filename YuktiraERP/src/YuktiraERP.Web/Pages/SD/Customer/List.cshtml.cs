using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.SD.Customer;
public class ListModel : PageModel
{
    private readonly IRepository<CustomerEntity, Guid> _repo;
    public ListModel(IRepository<CustomerEntity, Guid> repo) { _repo = repo; }
    public List<CustomerEntity> Customers { get; set; } = new();
    public async Task OnGetAsync() { Customers = await _repo.GetAllAsync(); }
}
