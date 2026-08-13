using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/sd/[controller]")]
[Authorize]
public class SOController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public SOController(YuktiraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.SalesOrders
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _db.SalesOrders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        var lines = await _db.SalesOrderLines.Where(l => l.SalesOrderId == id).ToListAsync();
        return Ok(new { data = order, lines, tenantId = _tenant.TenantId });
    }

    [HttpPost]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Create([FromBody] CreateSalesOrderRequest request)
    {
        var order = new SalesOrderEntity
        {
            OrderNumber = request.OrderNumber ?? $"SO-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..18],
            CustomerName = request.CustomerName ?? "",
            OrderDate = request.OrderDate == default ? DateTime.UtcNow : DateTime.SpecifyKind(request.OrderDate, DateTimeKind.Utc),
            ItemCount = request.Lines?.Count ?? 0,
            Amount = request.Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0,
            Status = "Pending"
        };
        _db.SalesOrders.Add(order);

        foreach (var line in request.Lines ?? new())
        {
            _db.SalesOrderLines.Add(new SalesOrderLineEntity
            {
                SalesOrderId = order.Id,
                MaterialName = line.MaterialName,
                Quantity = line.Quantity,
                UOM = line.UOM,
                UnitPrice = line.UnitPrice,
                TotalPrice = line.Quantity * line.UnitPrice
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = order.Id, orderNumber = order.OrderNumber, amount = order.Amount, tenantId = _tenant.TenantId });
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var order = await _db.SalesOrders.FindAsync(id);
        if (order == null) return NotFound();
        order.Status = "Confirmed";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var order = await _db.SalesOrders.FindAsync(id);
        if (order == null) return NotFound();
        order.Status = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }

    [HttpPost("{id:guid}/deliver")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Deliver(Guid id)
    {
        var order = await _db.SalesOrders.FindAsync(id);
        if (order == null) return NotFound();
        if (order.Status != "Confirmed")
            return BadRequest(new { error = "Only confirmed orders can be delivered" });

        var lines = await _db.SalesOrderLines.Where(l => l.SalesOrderId == id).ToListAsync();
        if (lines.Count > 0)
        {
            foreach (var line in lines)
            {
                var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == line.MaterialName || m.Code == line.MaterialName);
                if (material == null)
                    return BadRequest(new { error = $"Material not found for line: {line.MaterialName}" });
                if (material.Stock < line.Quantity)
                    return BadRequest(new { error = $"Insufficient stock for {line.MaterialName}: available {material.Stock}, required {line.Quantity}" });
            }

            foreach (var line in lines)
            {
                var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == line.MaterialName || m.Code == line.MaterialName);
                material!.Stock -= line.Quantity;
                material.UpdatedAt = DateTime.UtcNow;
            }
        }

        order.Status = "Delivered";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, orderId = order.Id, status = order.Status, tenantId = _tenant.TenantId });
    }
}

public class CreateSalesOrderRequest
{
    public string? OrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public List<SalesOrderLineRequest> Lines { get; set; } = new();
}

public class SalesOrderLineRequest
{
    public string MaterialName { get; set; } = "";
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "EA";
    public decimal UnitPrice { get; set; }
}
