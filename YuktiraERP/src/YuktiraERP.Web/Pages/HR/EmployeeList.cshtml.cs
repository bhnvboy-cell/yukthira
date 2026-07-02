using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR;
public class EmployeeListModel : PageModel
{
    private readonly IRepository<EmployeeEntity, Guid> _repo;
    public EmployeeListModel(IRepository<EmployeeEntity, Guid> repo) { _repo = repo; }
    public List<EmployeeEntity> Employees { get; set; } = new();
    public async Task OnGetAsync() { Employees = await _repo.GetAllAsync(); }
}
