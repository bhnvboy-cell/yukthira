using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/sd/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly IRepository<CustomerEntity, Guid> _repo;
    private readonly ITenantContext _tenant;

    public CustomerController(IRepository<CustomerEntity, Guid> repo, ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _repo.GetAllAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(new { data = item, tenantId = _tenant.TenantId });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerEntity model)
    {
        model.Id = Guid.NewGuid();
        await _repo.AddAsync(model);
        return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CustomerEntity model)
    {
        model.Id = id;
        await _repo.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repo.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
}
