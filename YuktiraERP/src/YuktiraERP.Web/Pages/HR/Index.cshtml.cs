using Microsoft.AspNetCore.Mvc.RazorPages;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR;

public class IndexModel : PageModel
{
    private readonly IRepository<EmployeeEntity, Guid> _empRepo;
    private readonly IRepository<AttendanceEntity, Guid> _attendanceRepo;
    private readonly IRepository<AppraisalEntity, Guid> _appraisalRepo;

    public List<EmployeeEntity> Employees { get; set; } = new();
    public List<AttendanceEntity> Attendances { get; set; } = new();
    public List<AppraisalEntity> Appraisals { get; set; } = new();

    public IndexModel(
        IRepository<EmployeeEntity, Guid> empRepo,
        IRepository<AttendanceEntity, Guid> attendanceRepo,
        IRepository<AppraisalEntity, Guid> appraisalRepo)
    {
        _empRepo = empRepo;
        _attendanceRepo = attendanceRepo;
        _appraisalRepo = appraisalRepo;
    }

    public async Task OnGetAsync()
    {
        Employees = await _empRepo.GetAllAsync();
        Attendances = await _attendanceRepo.GetAllAsync();
        Appraisals = await _appraisalRepo.GetAllAsync();
    }
}
