using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/mm/grn")]
[Authorize]
public class GRNController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public GRNController(YuktiraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.GoodsReceipts.Where(g => g.TenantId == _tenant.TenantId).OrderByDescending(g => g.Date).ToListAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _db.GoodsReceipts.FirstOrDefaultAsync(g => g.Id == id && g.TenantId == _tenant.TenantId);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }

    [HttpPost]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Create([FromBody] GoodsReceiptEntity model)
    {
        model.Id = Guid.NewGuid();
        model.TenantId = _tenant.TenantId;
        model.GrnNumber = string.IsNullOrEmpty(model.GrnNumber) ? $"GRN-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..18] : model.GrnNumber;
        model.Date = model.Date == default ? DateTime.UtcNow : DateTime.SpecifyKind(model.Date, DateTimeKind.Utc);
        model.Status = "Posted";

        if (decimal.TryParse(model.QtyReceived, out var qty) && qty > 0)
        {
            var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == model.MaterialName || m.Code == model.MaterialName);
            if (material != null)
            {
                var before = material.Stock;
                material.Stock += qty;
                material.UpdatedAt = DateTime.UtcNow;
                _db.StockMovements.Add(new StockMovementEntity
                {
                    TenantId = _tenant.TenantId,
                    DocumentNumber = model.GrnNumber,
                    MaterialName = material.Name,
                    MovementType = "Receipt",
                    Quantity = qty,
                    StockBefore = before,
                    StockAfter = material.Stock,
                    Reference = model.PoNumber,
                    Status = "Posted"
                });
            }
        }

        _db.GoodsReceipts.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = model.Id, grnNumber = model.GrnNumber, tenantId = _tenant.TenantId });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _db.GoodsReceipts.FirstOrDefaultAsync(g => g.Id == id && g.TenantId == _tenant.TenantId);
        if (item == null) return NotFound();

        if (decimal.TryParse(item.QtyReceived, out var qty) && qty > 0)
        {
            var material = await _db.MaterialMasters.FirstOrDefaultAsync(m => m.Name == item.MaterialName || m.Code == item.MaterialName);
            if (material != null)
            {
                material.Stock = Math.Max(0, material.Stock - qty);
                material.UpdatedAt = DateTime.UtcNow;
                _db.StockMovements.Add(new StockMovementEntity
                {
                    TenantId = _tenant.TenantId,
                    DocumentNumber = item.GrnNumber,
                    MaterialName = material.Name,
                    MovementType = "ReceiptReversal",
                    Quantity = qty,
                    StockBefore = material.Stock,
                    StockAfter = material.Stock,
                    Reference = item.PoNumber,
                    Status = "Posted"
                });
            }
        }

        _db.GoodsReceipts.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, tenantId = _tenant.TenantId });
    }
}
