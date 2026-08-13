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
    [HttpGet("projects/{id:guid}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var item = await _projects.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("projects")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateProject([FromBody] ProjectEntity model) { model.Id = Guid.NewGuid(); await _projects.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("projects/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] ProjectEntity model)
    {
        var exists = (await _projects.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _projects.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("projects/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var exists = (await _projects.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _projects.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("project-tasks")] public async Task<IActionResult> GetTasks() => Ok(new { data = await _tasks.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("project-tasks/{id:guid}")]
    public async Task<IActionResult> GetTask(Guid id)
    {
        var item = await _tasks.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("project-tasks")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateTask([FromBody] ProjectTaskEntity model) { model.Id = Guid.NewGuid(); await _tasks.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("project-tasks/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] ProjectTaskEntity model)
    {
        var exists = (await _tasks.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _tasks.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("project-tasks/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var exists = (await _tasks.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _tasks.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("timesheets")] public async Task<IActionResult> GetTimesheets() => Ok(new { data = await _timesheets.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("timesheets/{id:guid}")]
    public async Task<IActionResult> GetTimesheet(Guid id)
    {
        var item = await _timesheets.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("timesheets")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateTimesheet([FromBody] TimesheetEntryEntity model) { model.Id = Guid.NewGuid(); await _timesheets.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("timesheets/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateTimesheet(Guid id, [FromBody] TimesheetEntryEntity model)
    {
        var exists = (await _timesheets.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _timesheets.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("timesheets/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteTimesheet(Guid id)
    {
        var exists = (await _timesheets.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _timesheets.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
}
