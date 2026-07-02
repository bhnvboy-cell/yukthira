using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR;
public class PayrollHistoryModel : PageModel
{
    private readonly IRepository<PayrollEntryEntity, Guid> _repo;
    public PayrollHistoryModel(IRepository<PayrollEntryEntity, Guid> repo) { _repo = repo; }
    public List<PayrollEntryEntity> PayrollEntries { get; set; } = new();
    public async Task OnGetAsync() { PayrollEntries = await _repo.GetAllAsync(); }
}
