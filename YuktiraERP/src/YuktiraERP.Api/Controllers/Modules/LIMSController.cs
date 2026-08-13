using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/lims")]
[Authorize]
public class LIMSController : ControllerBase
{
    private readonly IRepository<SampleEntity, Guid> _samples;
    private readonly IRepository<TestResultEntity, Guid> _results;
    private readonly IRepository<SpecificationEntity, Guid> _specifications;
    private readonly IRepository<InstrumentEntity, Guid> _instruments;
    private readonly ITenantContext _tenant;

    public LIMSController(
        IRepository<SampleEntity, Guid> samples,
        IRepository<TestResultEntity, Guid> results,
        IRepository<SpecificationEntity, Guid> specifications,
        IRepository<InstrumentEntity, Guid> instruments,
        ITenantContext tenant)
    {
        _samples = samples;
        _results = results;
        _specifications = specifications;
        _instruments = instruments;
        _tenant = tenant;
    }

    [HttpGet("samples")] public async Task<IActionResult> GetSamples() => Ok(new { data = await _samples.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("samples/{id:guid}")]
    public async Task<IActionResult> GetSample(Guid id)
    {
        var item = await _samples.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("samples")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateSample([FromBody] SampleEntity model) { model.Id = Guid.NewGuid(); await _samples.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("samples/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateSample(Guid id, [FromBody] SampleEntity model)
    {
        var exists = (await _samples.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _samples.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("samples/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteSample(Guid id)
    {
        var exists = (await _samples.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _samples.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("results")] public async Task<IActionResult> GetResults() => Ok(new { data = await _results.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("results/{id:guid}")]
    public async Task<IActionResult> GetResult(Guid id)
    {
        var item = await _results.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("results")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateResult([FromBody] TestResultEntity model) { model.Id = Guid.NewGuid(); await _results.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("results/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateResult(Guid id, [FromBody] TestResultEntity model)
    {
        var exists = (await _results.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _results.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("results/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteResult(Guid id)
    {
        var exists = (await _results.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _results.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("specifications")] public async Task<IActionResult> GetSpecifications() => Ok(new { data = await _specifications.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("specifications/{id:guid}")]
    public async Task<IActionResult> GetSpecification(Guid id)
    {
        var item = await _specifications.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("specifications")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateSpecification([FromBody] SpecificationEntity model) { model.Id = Guid.NewGuid(); await _specifications.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("specifications/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateSpecification(Guid id, [FromBody] SpecificationEntity model)
    {
        var exists = (await _specifications.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _specifications.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("specifications/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteSpecification(Guid id)
    {
        var exists = (await _specifications.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _specifications.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpGet("instruments")] public async Task<IActionResult> GetInstruments() => Ok(new { data = await _instruments.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpGet("instruments/{id:guid}")]
    public async Task<IActionResult> GetInstrument(Guid id)
    {
        var item = await _instruments.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }
    [HttpPost("instruments")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateInstrument([FromBody] InstrumentEntity model) { model.Id = Guid.NewGuid(); await _instruments.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
    [HttpPut("instruments/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateInstrument(Guid id, [FromBody] InstrumentEntity model)
    {
        var exists = (await _instruments.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        model.Id = id;
        await _instruments.UpdateAsync(model);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
    [HttpDelete("instruments/{id:guid}")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DeleteInstrument(Guid id)
    {
        var exists = (await _instruments.FindAsync(e => e.Id == id)).Count > 0;
        if (!exists) return NotFound();
        await _instruments.DeleteAsync(id);
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
}
