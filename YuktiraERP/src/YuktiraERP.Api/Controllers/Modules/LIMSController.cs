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
    [HttpPost("samples")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateSample([FromBody] SampleEntity model) { model.Id = Guid.NewGuid(); await _samples.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("results")] public async Task<IActionResult> GetResults() => Ok(new { data = await _results.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("results")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateResult([FromBody] TestResultEntity model) { model.Id = Guid.NewGuid(); await _results.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("specifications")] public async Task<IActionResult> GetSpecifications() => Ok(new { data = await _specifications.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("specifications")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateSpecification([FromBody] SpecificationEntity model) { model.Id = Guid.NewGuid(); await _specifications.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }

    [HttpGet("instruments")] public async Task<IActionResult> GetInstruments() => Ok(new { data = await _instruments.GetAllAsync(), tenantId = _tenant.TenantId });
    [HttpPost("instruments")] [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateInstrument([FromBody] InstrumentEntity model) { model.Id = Guid.NewGuid(); await _instruments.AddAsync(model); return Ok(new { success = true, id = model.Id, tenantId = _tenant.TenantId }); }
}
