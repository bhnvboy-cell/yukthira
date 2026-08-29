using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace YuktiraERP.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WorkflowController : ControllerBase
{
    [HttpGet("chains")]
    public IActionResult GetChains() => Ok(new List<object>());

    [HttpGet("chains/{id}")]
    public IActionResult GetChain(Guid id) => NotFound();

    [HttpPost("chains")]
    public IActionResult CreateChain([FromBody] object body) => Ok(new { success = true });

    [HttpGet("instances")]
    public IActionResult GetInstances() => Ok(new List<object>());

    [HttpPost("instances/{chainId}/advance")]
    public IActionResult AdvanceInstance(Guid chainId, [FromBody] object body) => Ok(new { success = true });
}
