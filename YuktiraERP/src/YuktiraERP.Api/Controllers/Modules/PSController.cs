using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/ps")]
[Authorize]
public class PSController : ControllerBase
{
    private readonly IRepository<ProjectEntity, Guid> _projects;
    private readonly IRepository<ProjectTaskEntity, Guid> _tasks;
    private readonly IRepository<TimesheetEntryEntity, Guid> _timesheets;
    private readonly ITenantContext _tenant;

    public PSController(
        IRepository<ProjectEntity, Guid> projects,
        IRepository<ProjectTaskEntity, Guid> tasks,
        IRepository<TimesheetEntryEntity, Guid> timesheets,
        ITenantContext tenant)
    {
        _projects = projects;
        _tasks = tasks;
        _timesheets = timesheets;
        _tenant = tenant;
    }

    [HttpGet("projects")] public async Task<IActionResult> GetProjects() => Ok(new { data = await _projects.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("projects")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateProject([FromBody] ProjectEntity model) { model.Id = Guid.NewGuid(); await _projects.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("project-tasks")] public async Task<IActionResult> GetTasks() => Ok(new { data = await _tasks.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("project-tasks")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateTask([FromBody] ProjectTaskEntity model) { model.Id = Guid.NewGuid(); await _tasks.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("timesheets")] public async Task<IActionResult> GetTimesheets() => Ok(new { data = await _timesheets.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("timesheets")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateTimesheet([FromBody] TimesheetEntryEntity model) { model.Id = Guid.NewGuid(); await _timesheets.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
}
