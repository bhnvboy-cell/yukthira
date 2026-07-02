using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;
namespace YuktiraERP.Web.Pages.HR;
public class AttendanceReportModel : PageModel
{
    private readonly IRepository<AttendanceEntity, Guid> _repo;
    public AttendanceReportModel(IRepository<AttendanceEntity, Guid> repo) { _repo = repo; }
    public List<AttendanceEntity> Records { get; set; } = new();
    public async Task OnGetAsync() { Records = await _repo.GetAllAsync(); }
}
