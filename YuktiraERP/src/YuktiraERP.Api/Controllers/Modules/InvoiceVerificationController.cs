using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/mm/invoice-verification")]
[Authorize]
public class InvoiceVerificationController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly ITenantContext _tenant;

    public InvoiceVerificationController(YuktiraDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.InvoiceVerifications.OrderByDescending(v => v.Date).ToListAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _db.InvoiceVerifications.FindAsync(id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }

    [HttpPost]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> Create([FromBody] InvoiceVerificationEntity model)
    {
        model.Id = Guid.NewGuid();
        model.InvoiceNumber = string.IsNullOrEmpty(model.InvoiceNumber) ? $"IV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..18] : model.InvoiceNumber;
        model.Date = model.Date == default ? DateTime.UtcNow : DateTime.SpecifyKind(model.Date, DateTimeKind.Utc);
        model.Status = "Verified";

        if (!string.IsNullOrEmpty(model.PoNumber))
        {
            var po = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.PoNumber == model.PoNumber);
            if (po != null)
            {
                po.Status = "Invoiced";
                po.UpdatedAt = DateTime.UtcNow;
                if (model.MatchedAmount == 0) model.MatchedAmount = Math.Min(model.Amount, po.Amount);
            }
        }

        if (model.Amount > 0)
        {
            _db.APEntries.Add(new APEntryEntity
            {
                Id = Guid.NewGuid(),
                DocumentNumber = $"AP-{model.InvoiceNumber}",
                Date = model.Date,
                VendorName = model.VendorName,
                Amount = model.Amount,
                Status = "Open"
            });
        }

        _db.InvoiceVerifications.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = model.Id, invoiceNumber = model.InvoiceNumber, status = model.Status, tenantId = _tenant.TenantId });
    }
}
