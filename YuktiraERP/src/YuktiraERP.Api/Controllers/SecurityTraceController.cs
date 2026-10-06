using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/security/trace")]
[Authorize]
[Authorize(Policy = "AdminOrAbove")]
public class SecurityTraceController : ControllerBase
{
    private readonly IAuthorizationTraceService _trace;
    private readonly ITenantContext _tenant;

    public SecurityTraceController(IAuthorizationTraceService trace, ITenantContext tenant)
    {
        _trace = trace;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? decision, [FromQuery] string? resourceType, [FromQuery] string? resource, [FromQuery] string? userName, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = BuildQuery(decision, resourceType, resource, userName, from, to, page, pageSize);
        var result = await _trace.QueryAsync(query, _tenant.TenantId);
        return Ok(new { entries = result.Entries, totalCount = result.TotalCount, page = result.Page, pageSize = result.PageSize, tenantId = _tenant.TenantId });
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? decision, [FromQuery] string? resourceType, [FromQuery] string? resource, [FromQuery] string? userName, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = BuildQuery(decision, resourceType, resource, userName, from, to, 1, 50);
        var stream = new MemoryStream();
        await _trace.ExportToCsvAsync(_tenant.TenantId, stream, query);
        stream.Position = 0;
        return File(stream, "text/csv", $"authz_trace_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpDelete]
    public async Task<IActionResult> Purge([FromQuery] int olderThanDays = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Clamp(olderThanDays, 1, 365));
        var purged = await _trace.PurgeAsync(_tenant.TenantId, cutoff);
        return Ok(new { purged });
    }

    private static AuthorizationTraceQuery BuildQuery(string? decision, string? resourceType, string? resource, string? userName, DateTime? from, DateTime? to, int page, int pageSize) => new()
    {
        Decision = decision,
        ResourceType = resourceType,
        Resource = resource,
        UserName = userName,
        From = from,
        To = to,
        Page = page,
        PageSize = pageSize
    };
}
