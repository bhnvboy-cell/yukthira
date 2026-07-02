using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Domain.Transaction;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly ITransactionCodeService _service;
    private readonly IAuditService _audit;

    public TransactionController(ITransactionCodeService service, IAuditService audit)
    {
        _service = service;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? module, [FromQuery] string? group, [FromQuery] string? search)
    {
        TransactionGroup? tg = null;
        if (!string.IsNullOrEmpty(group) && Enum.TryParse<TransactionGroup>(group, out var parsed)) tg = parsed;
        return Ok(await _service.GetAllAsync(module, tg, search));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _service.GetByCodeAsync(code);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> Create([FromBody] TransactionCodeDto dto)
    {
        var result = await _service.CreateAsync(dto);
        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = GetTenantId(),
            ModuleName = "Transaction",
            ActionType = ActionType.Create,
            EntityName = "TransactionCode",
            EntityId = result.Id.ToString(),
            NewValue = result.Code
        });
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TransactionCodeDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        if (result is null) return NotFound();
        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = GetTenantId(),
            ModuleName = "Transaction",
            ActionType = ActionType.Update,
            EntityName = "TransactionCode",
            EntityId = id.ToString(),
            NewValue = result.Code
        });
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return BadRequest(new { error = "Cannot delete system transactions" });
        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = GetUserId(),
            TenantId = GetTenantId(),
            ModuleName = "Transaction",
            ActionType = ActionType.Delete,
            EntityName = "TransactionCode",
            EntityId = id.ToString()
        });
        return Ok(new { success = true });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        return Ok(await _service.SearchAsync(q));
    }

    [HttpPost("{code}/execute")]
    public async Task<IActionResult> Execute(string code, [FromBody] ExecuteTransactionRequest? request = null)
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _service.ExecuteAsync(code, userId, tenantId, ip, request?.Parameters);
        await _audit.LogAsync(new AuditEntryDto
        {
            UserId = userId,
            TenantId = tenantId,
            ModuleName = "Transaction",
            ActionType = ActionType.ApiCall,
            EntityName = "TransactionExecute",
            EntityId = code,
            IpAddress = ip,
            Details = $"Status: {result.Status}, Duration: {result.DurationMs}ms"
        });
        return result.Status switch
        {
            ExecutionStatus.Success => Ok(result),
            ExecutionStatus.NotFound => NotFound(result),
            _ => BadRequest(result)
        };
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
    {
        return Ok(await _service.GetFavoritesAsync(GetUserId()));
    }

    [HttpPost("favorites/{transactionCodeId:guid}")]
    public async Task<IActionResult> ToggleFavorite(Guid transactionCodeId)
    {
        await _service.ToggleFavoriteAsync(GetUserId(), transactionCodeId);
        return Ok(new { success = true });
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 10)
    {
        return Ok(await _service.GetRecentAsync(GetUserId(), count));
    }

    [HttpGet("permitted")]
    public async Task<IActionResult> GetPermitted()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        return Ok(await _service.GetPermittedCodesAsync(GetUserId(), role));
    }

    [HttpPost("validate-access/{code}")]
    public async Task<IActionResult> ValidateAccess(string code)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var allowed = await _service.ValidateAccessAsync(code, GetUserId(), role);
        return Ok(new { allowed });
    }

    [HttpGet("permissions/{transactionCodeId:guid}")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> GetPermissions(Guid transactionCodeId)
    {
        return Ok(await _service.GetPermissionsAsync(transactionCodeId));
    }

    [HttpPost("permissions")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> SetPermission([FromBody] TransactionPermissionDto dto)
    {
        var result = await _service.SetPermissionAsync(dto);
        return Ok(result);
    }

    [HttpGet("log")]
    [Authorize(Policy = "AdminOrAbove")]
    public async Task<IActionResult> GetLog([FromQuery] Guid? userId, [FromQuery] string? code, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        return Ok(await _service.GetLogAsync(userId, code, from, to, page, pageSize));
    }

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
    private Guid? GetTenantId()
    {
        var claim = User.FindFirst("TenantId")?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }
}
