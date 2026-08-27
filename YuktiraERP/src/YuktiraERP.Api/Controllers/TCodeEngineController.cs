using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TCodeEngineController : ControllerBase
{
    private readonly ITCodeLayoutRegistry _registry;

    public TCodeEngineController(ITCodeLayoutRegistry registry) => _registry = registry;

    [HttpGet("layout/{tcode}")]
    public IActionResult GetLayout(string tcode)
    {
        var config = _registry.Get(tcode);
        if (config is null) return NotFound(new { error = $"No layout config for '{tcode}'" });
        return Ok(config);
    }

    [HttpGet("layouts")]
    public IActionResult GetAllLayouts()
    {
        return Ok(_registry.GetAll().Select(c => new { c.TCode, c.Title, c.Module, c.Icon }));
    }

    [HttpPost("layout/{tcode}/data")]
    public IActionResult GetTableData(string tcode, [FromBody] object? filter = null)
    {
        var config = _registry.Get(tcode);
        if (config is null) return NotFound();
        return Ok(new { columns = config.Columns, rows = new object[0] });
    }

    [HttpPost("layout/{tcode}/action")]
    public IActionResult ExecuteAction(string tcode, [FromBody] TCodeActionRequest request)
    {
        var config = _registry.Get(tcode);
        if (config is null) return NotFound();
        return Ok(new { success = true, action = request.Action, tcode, message = $"Action '{request.Action}' executed for {tcode}" });
    }
}

public class TCodeActionRequest
{
    public string Action { get; set; } = "";
    public Dictionary<string, object>? Payload { get; set; }
}
