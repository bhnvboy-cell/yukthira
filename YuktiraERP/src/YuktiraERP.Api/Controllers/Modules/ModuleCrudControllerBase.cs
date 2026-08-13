using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Authorize]
public abstract class ModuleCrudControllerBase<TEntity> : ControllerBase
    where TEntity : EntityBase, new()
{
    protected readonly IRepository<TEntity, Guid> Repo;
    protected readonly ITenantContext Tenant;

    protected ModuleCrudControllerBase(IRepository<TEntity, Guid> repo, ITenantContext tenant)
    {
        Repo = repo;
        Tenant = tenant;
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetAll()
    {
        var items = await Repo.GetAllAsync();
        return Ok(new { data = items, tenantId = Tenant.TenantId });
    }

    [HttpGet("{id:guid}")]
    public virtual async Task<IActionResult> GetById(Guid id)
    {
        var item = await Repo.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = Tenant.TenantId });
    }

    [HttpPost]
    [Authorize(Policy = "PowerUserOrAbove")]
    public virtual async Task<IActionResult> Create([FromBody] TEntity model)
    {
        model.Id = Guid.NewGuid();
        await Repo.AddAsync(model);
        return Ok(new { success = true, id = model.Id, tenantId = Tenant.TenantId });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] TEntity model)
    {
        var exists = (await Repo.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await Repo.UpdateAsync(model);
        return Ok(new { success = true, tenantId = Tenant.TenantId });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public virtual async Task<IActionResult> Delete(Guid id)
    {
        var exists = (await Repo.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await Repo.DeleteAsync(id);
        return Ok(new { success = true, tenantId = Tenant.TenantId });
    }
}
