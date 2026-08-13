using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/admin/tenants")]
[Authorize(Policy = "AdminOrAbove")]
public class AdminTenantController : ControllerBase
{
    private readonly IRepository<TenantEntity, Guid> _tenants;

    public AdminTenantController(IRepository<TenantEntity, Guid> tenants) => _tenants = tenants;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(new { data = await _tenants.GetAllAsync() });

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _tenants.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TenantEntity model)
    {
        model.Id = Guid.NewGuid();
        await _tenants.AddAsync(model);
        return Ok(new { success = true, id = model.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TenantEntity model)
    {
        model.Id = id;
        await _tenants.UpdateAsync(model);
        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _tenants.DeleteAsync(id);
        return Ok(new { success = true });
    }
}
