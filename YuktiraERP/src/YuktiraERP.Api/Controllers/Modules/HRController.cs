using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/hr/[controller]")]
[Authorize]
public class HRController : ControllerBase
{
    private readonly ITenantContext _tenant;
    public HRController(ITenantContext tenant) => _tenant = tenant;
    [HttpGet("employees")] public IActionResult GetEmployees() => Ok(new { data = new[] { new { Code = "EMP-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("employees")] public IActionResult CreateEmployee([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("leave")] public IActionResult GetLeave() => Ok(new { data = new[] { new { LeaveId = "LV-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("leave")] public IActionResult CreateLeave([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("payroll")] public IActionResult GetPayroll() => Ok(new { data = new[] { new { PayrollId = "PRL-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("payroll")] public IActionResult CreatePayroll([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("attendance")] public IActionResult GetAttendance() => Ok(new { data = new[] { new { AttendanceId = "ATT-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("attendance")] public IActionResult CreateAttendance([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
    [HttpGet("appraisal")] public IActionResult GetAppraisal() => Ok(new { data = new[] { new { AppraisalId = "APR-001" } }, tenantId = _tenant.TenantId });
    [HttpPost("appraisal")] public IActionResult CreateAppraisal([FromBody] object model) => Ok(new { success = true, tenantId = _tenant.TenantId });
}
