using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Web.Pages.HR;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IRepository<EmployeeEntity, Guid> _empRepo;
    private readonly IRepository<LeaveRequestEntity, Guid> _leaveRepo;
    private readonly IRepository<PayrollEntryEntity, Guid> _payrollRepo;
    private readonly IRepository<AttendanceEntity, Guid> _attendanceRepo;
    private readonly IRepository<AppraisalEntity, Guid> _appraisalRepo;
    private readonly IRepository<OrgUnitEntity, Guid> _orgUnitRepo;
    private readonly IRepository<TimeEntryEntity, Guid> _timeEntryRepo;
    private readonly IRepository<RecruitmentEntity, Guid> _recruitmentRepo;

    public List<EmployeeEntity> Employees { get; set; } = new();
    public List<LeaveRequestEntity> LeaveRequests { get; set; } = new();
    public List<PayrollEntryEntity> PayrollEntries { get; set; } = new();
    public List<AttendanceEntity> Attendances { get; set; } = new();
    public List<AppraisalEntity> Appraisals { get; set; } = new();
    public List<OrgUnitEntity> OrgUnits { get; set; } = new();
    public List<TimeEntryEntity> TimeEntries { get; set; } = new();
    public List<RecruitmentEntity> Recruitments { get; set; } = new();

    public int TotalActiveHeadcount { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int OpenRequisitions { get; set; }
    public decimal MonthlyPayrollTotal { get; set; }
    public decimal AbsenteeismRate { get; set; }

    public IndexModel(
        IRepository<EmployeeEntity, Guid> empRepo,
        IRepository<LeaveRequestEntity, Guid> leaveRepo,
        IRepository<PayrollEntryEntity, Guid> payrollRepo,
        IRepository<AttendanceEntity, Guid> attendanceRepo,
        IRepository<AppraisalEntity, Guid> appraisalRepo,
        IRepository<OrgUnitEntity, Guid> orgUnitRepo,
        IRepository<TimeEntryEntity, Guid> timeEntryRepo,
        IRepository<RecruitmentEntity, Guid> recruitmentRepo)
    {
        _empRepo = empRepo;
        _leaveRepo = leaveRepo;
        _payrollRepo = payrollRepo;
        _attendanceRepo = attendanceRepo;
        _appraisalRepo = appraisalRepo;
        _orgUnitRepo = orgUnitRepo;
        _timeEntryRepo = timeEntryRepo;
        _recruitmentRepo = recruitmentRepo;
    }

    public async Task OnGetAsync()
    {
        Employees = await _empRepo.GetAllAsync();
        LeaveRequests = await _leaveRepo.GetAllAsync();
        PayrollEntries = await _payrollRepo.GetAllAsync();
        Attendances = await _attendanceRepo.GetAllAsync();
        Appraisals = await _appraisalRepo.GetAllAsync();
        OrgUnits = await _orgUnitRepo.GetAllAsync();
        TimeEntries = await _timeEntryRepo.GetAllAsync();
        Recruitments = await _recruitmentRepo.GetAllAsync();

        TotalActiveHeadcount = Employees.Count(e => e.Status == "Active");
        PendingLeaveRequests = LeaveRequests.Count(l => l.Status == "Pending");
        OpenRequisitions = Recruitments.Count(r => r.Status == "Open");

        var now = DateTime.UtcNow;
        MonthlyPayrollTotal = PayrollEntries
            .Where(p => p.Status == "Paid" && DateTime.TryParse(p.Period, out var pp) && pp.Month == now.Month && pp.Year == now.Year)
            .Sum(p => p.NetPay);

        var totalAttendance = Attendances.Count;
        var absentCount = Attendances.Count(a => a.Status == "Absent");
        AbsenteeismRate = totalAttendance > 0 ? Math.Round((decimal)absentCount / totalAttendance * 100, 1) : 0;
    }
}
