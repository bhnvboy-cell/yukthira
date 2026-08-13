using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;
[ApiController]
[Route("api/admin/system-config")]
[Authorize(Policy = "AdminOrAbove")]
public class AdminSystemConfigController : ControllerBase
{
    private readonly IRepository<SystemConfigEntity, Guid> _repo;
    private readonly ITenantContext _tenant;
    public AdminSystemConfigController(IRepository<SystemConfigEntity, Guid> repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var all = await _repo.GetAllAsync();
        var data = all
            .Select(c => new { c.Key, c.Value, c.Description, c.Module })
            .OrderBy(c => c.Module)
            .ThenBy(c => c.Key);
        return Ok(new { data, tenantId = _tenant.TenantId });
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateConfigModel model)
    {
        var config = (await _repo.GetAllAsync()).FirstOrDefault(c => c.Key == key);
        if (config == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(model.Value)) config.Value = model.Value;
        if (!string.IsNullOrWhiteSpace(model.Description)) config.Description = model.Description;
        config.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(config);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SystemConfigEntity model)
    {
        model.Id = Guid.NewGuid();
        await _repo.AddAsync(model);
        return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId });
    }

    public class UpdateConfigModel { public string? Value { get; set; } public string? Description { get; set; } }
}
