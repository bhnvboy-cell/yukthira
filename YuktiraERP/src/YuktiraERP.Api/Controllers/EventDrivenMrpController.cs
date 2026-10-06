using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Services;

namespace YuktiraERP.Api.Controllers;

public class MrpSimulationRequest
{
    public string EventType { get; set; } = "order.created";
    public List<string> MaterialCodes { get; set; } = new();
    public Dictionary<string, decimal> Quantities { get; set; } = new();
    public string ReferenceId { get; set; } = "";
}

[ApiController]
[Route("api/mrp/event-engine")]
[Authorize]
public class EventDrivenMrpController : ControllerBase
{
    private readonly EventDrivenMrpEngine _engine;
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public EventDrivenMrpController(
        EventDrivenMrpEngine engine,
        YuktiraDbContext db,
        ITenantContext tenant)
    {
        _engine = engine;
        _db = db;
        _tenant = tenant;
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        var stats = _engine.GetStats();
        return Ok(new
        {
            enabled = stats.Enabled,
            registeredListeners = stats.RegisteredListeners,
            eventsHandled = stats.EventsHandled,
            draftPurchaseOrdersCreated = stats.DraftPurchaseOrdersCreated,
            draftProductionOrdersCreated = stats.DraftProductionOrdersCreated,
            shortagesDetected = stats.ShortagesDetected,
            queuedEvents = stats.QueuedEvents,
            queueDepth = _engine.QueueDepth,
            drainLoopRunning = stats.DrainLoopRunning,
            lastEventAt = stats.LastEventAt,
            recentEvents = stats.RecentEvents
        });
    }

    [HttpGet("proposals")]
    public async Task<IActionResult> Proposals([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        limit = limit < 1 ? 50 : Math.Min(limit, 500);
        var tenantId = _tenant.TenantId;

        var draftPos = await _db.PurchaseOrders.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Status == "DRAFT")
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new
            {
                p.Id,
                p.PoNumber,
                p.VendorName,
                p.VendorCode,
                p.ItemName,
                p.Quantity,
                p.Amount,
                p.TotalAmount,
                p.Status,
                p.CreatedAt
            })
            .ToListAsync(ct);

        var draftProduction = await _db.ProductionOrders.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Status == "PLANNED")
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new
            {
                p.Id,
                p.OrderNumber,
                p.OrderType,
                p.ProductName,
                p.MaterialCode,
                p.Quantity,
                p.BaseUOM,
                p.Status,
                p.StartDate,
                p.EndDate,
                p.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            tenantId,
            draftPurchaseOrderCount = draftPos.Count,
            draftProductionOrderCount = draftProduction.Count,
            draftPurchaseOrders = draftPos,
            draftProductionOrders = draftProduction
        });
    }

    [HttpPost("simulate")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Simulate([FromBody] MrpSimulationRequest request, CancellationToken ct)
    {
        request ??= new MrpSimulationRequest();
        if (request.MaterialCodes.Count == 0 && request.Quantities.Count == 0)
        {
            return BadRequest(new { message = "Provide materialCodes or quantities to simulate" });
        }

        var evt = new MrpDomainEvent
        {
            EventType = string.IsNullOrWhiteSpace(request.EventType) ? "order.created" : request.EventType,
            TenantId = _tenant.TenantId,
            OccurredAt = DateTime.UtcNow,
            MaterialCodes = request.MaterialCodes,
            Quantities = request.Quantities,
            ReferenceId = request.ReferenceId,
            SourceUserId = GetUserId()
        };

        var result = await _engine.HandleAsync(evt, ct);
        return Ok(new
        {
            success = true,
            eventId = result.EventId,
            eventType = result.EventType,
            tenantId = result.TenantId,
            processedAt = result.ProcessedAt,
            evaluatedMaterials = result.EvaluatedMaterials,
            shortages = result.Shortages,
            draftPurchaseOrders = result.DraftPurchaseOrders,
            draftProductionOrders = result.DraftProductionOrders,
            skippedReasons = result.SkippedReasons,
            totals = result.Totals,
            stats = _engine.GetStats()
        });
    }

    private string? GetUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
