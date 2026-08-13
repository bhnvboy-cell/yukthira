using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/hr")]
[Authorize]
public class HRController : ControllerBase
{
    private readonly IRepository<EmployeeEntity, Guid> _employees;
    private readonly IRepository<LeaveRequestEntity, Guid> _leave;
    private readonly IRepository<PayrollEntryEntity, Guid> _payroll;
    private readonly IRepository<AttendanceEntity, Guid> _attendance;
    private readonly IRepository<AppraisalEntity, Guid> _appraisal;
    private readonly IPayrollService _payrollCalc;
    private readonly ITenantContext _tenant;

    public HRController(
        IRepository<EmployeeEntity, Guid> employees,
        IRepository<LeaveRequestEntity, Guid> leave,
        IRepository<PayrollEntryEntity, Guid> payroll,
        IRepository<AttendanceEntity, Guid> attendance,
        IRepository<AppraisalEntity, Guid> appraisal,
        IPayrollService payrollCalc,
        ITenantContext tenant)
    {
        _employees = employees;
        _leave = leave;
        _payroll = payroll;
        _attendance = attendance;
        _appraisal = appraisal;
        _payrollCalc = payrollCalc;
        _tenant = tenant;
    }

    [HttpGet("employees")] public async Task<IActionResult> GetEmployees() => Ok(new { data = await _employees.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("employees")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateEmployee([FromBody] EmployeeEntity model) { model.Id = Guid.NewGuid(); await _employees.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("leave")] public async Task<IActionResult> GetLeave() => Ok(new { data = await _leave.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("leave")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateLeave([FromBody] LeaveRequestEntity model) { model.Id = Guid.NewGuid(); await _leave.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("payroll")] public async Task<IActionResult> GetPayroll() => Ok(new { data = await _payroll.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("payroll")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreatePayroll([FromBody] PayrollEntryEntity model) { model.Id = Guid.NewGuid(); await _payroll.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpPost("payroll/calculate")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CalculatePayroll([FromBody] PayrollCalculationRequest request, [FromQuery] string period = "")
    {
        var result = await _payrollCalc.CalculatePayrollAsync(request, period);
        return Ok(result);
    }

    [HttpGet("attendance")] public async Task<IActionResult> GetAttendance() => Ok(new { data = await _attendance.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("attendance")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateAttendance([FromBody] AttendanceEntity model) { model.Id = Guid.NewGuid(); await _attendance.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("appraisal")] public async Task<IActionResult> GetAppraisal() => Ok(new { data = await _appraisal.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("appraisal")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateAppraisal([FromBody] AppraisalEntity model) { model.Id = Guid.NewGuid(); await _appraisal.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
}
