using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/qm/mic")]
[Authorize]
public class MicController : ControllerBase
{
    private readonly IMicService _micService;
    private readonly ITenantContext _tenant;

    public MicController(IMicService micService, ITenantContext tenant)
    {
        _micService = micService;
        _tenant = tenant;
    }

    [HttpPost]
    [Authorize(Policy = "QM_ADMIN")]
    public async Task<IActionResult> Create([FromBody] MicCreateRequest request)
    {
        var result = await _micService.CreateMicAsync(request, _tenant.TenantId);
        return Ok(new { success = true, data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? plantId,
        [FromQuery] string? status,
        [FromQuery] string? characteristicCode,
        [FromQuery] bool? isQuantitative)
    {
        var filter = new MicSearchFilter
        {
            PlantId = plantId,
            Status = status,
            CharacteristicCode = characteristicCode,
            IsQuantitative = isQuantitative
        };
        var result = await _micService.SearchMicsAsync(filter, _tenant.TenantId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("check-exists")]
    public async Task<IActionResult> CheckExists(
        [FromQuery] string plant,
        [FromQuery] string code)
    {
        var exists = await _micService.CheckMicExistsAsync(plant, code, _tenant.TenantId);
        return Ok(new { exists, tenantId = _tenant.TenantId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _micService.GetMicByIdAsync(id, _tenant.TenantId);
        return result == null ? NotFound() : Ok(new { data = result, tenantId = _tenant.TenantId });
    }
}
