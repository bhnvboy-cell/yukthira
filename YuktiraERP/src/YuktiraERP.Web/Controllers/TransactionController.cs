using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YuktiraERP.Core.Domain.Common;
using YuktiraERP.Core.Domain.Transaction;
using YuktiraERP.Core.Interfaces;

namespace YuktiraERP.Web.Controllers;

[ApiController]
[Route("api/transaction")]
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

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        return Ok(await _service.SearchAsync(q));
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

    private Guid GetUserId() => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
    private Guid? GetTenantId()
    {
        var claim = User.FindFirst("TenantId")?.Value;
        return claim is not null && Guid.TryParse(claim, out var id) ? id : null;
    }
}
