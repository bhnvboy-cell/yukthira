using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Dtos;
using YuktiraERP.Core.Enums;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/v1/ai/nlquery")]
[Authorize]
public class NlQueryController : ControllerBase
{
    private readonly INaturalLanguageQueryEngine _nlEngine;
    private readonly ITenantContext _tenant;

    public NlQueryController(INaturalLanguageQueryEngine nlEngine, ITenantContext tenant)
    {
        _nlEngine = nlEngine;
        _tenant = tenant;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteQuery([FromBody] NlQueryRequest request)
    {
        request.TenantId = _tenant.TenantId;
        var result = await _nlEngine.ExecuteQueryAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string q = "")
    {
        var suggestions = await _nlEngine.GetSuggestionsAsync(q, _tenant.TenantId);
        return Ok(suggestions);
    }

    [HttpGet("entity-types")]
    public IActionResult GetEntityTypes()
    {
        var types = Enum.GetValues(typeof(NlQueryEntityType))
            .Cast<NlQueryEntityType>()
            .Select(v => new { Value = (int)v, Name = v.ToString() });
        return Ok(types);
    }
}
