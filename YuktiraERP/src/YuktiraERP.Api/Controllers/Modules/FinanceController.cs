using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Api.Controllers.Modules;

[ApiController]
[Route("api/fi/[controller]")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly YuktiraDbContext _db;
    private readonly IAccountingService _accounting;
    private readonly ITenantContext _tenant;

    public FinanceController(YuktiraDbContext db, IAccountingService accounting, ITenantContext tenant)
    {
        _db = db;
        _accounting = accounting;
        _tenant = tenant;
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _db.Accounts.Where(a => a.IsActive).OrderBy(a => a.AccountCode).ToListAsync();
        return Ok(new { data = accounts, tenantId = _tenant.TenantId });
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger()
    {
        var entries = await _db.GeneralLedgerEntries
            .OrderByDescending(e => e.EntryDate)
            .Take(500)
            .ToListAsync();
        return Ok(new { data = entries, tenantId = _tenant.TenantId });
    }

    [HttpPost("journal")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PostJournal([FromBody] JournalPostingRequest request)
    {
        try
        {
            await _accounting.PostJournalEntryAsync(request);
            return Ok(new { success = true, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] DateTime? asOfDate)
    {
        var result = await _accounting.GetTrialBalanceAsync(asOfDate);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("profit-loss")]
    public async Task<IActionResult> ProfitLoss([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.Today.AddMonths(-1);
        var toDate = to ?? DateTime.Today;
        var result = await _accounting.GetProfitAndLossAsync(fromDate, toDate);
        return Ok(new { data = result, fromDate, toDate, tenantId = _tenant.TenantId });
    }

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet([FromQuery] DateTime? asOfDate)
    {
        var date = asOfDate ?? DateTime.Today;
        var result = await _accounting.GetBalanceSheetAsync(date);
        return Ok(new { data = result, asOfDate = date, tenantId = _tenant.TenantId });
    }

    [HttpGet("ap")]
    public async Task<IActionResult> GetAP()
    {
        var items = await _db.APEntries.OrderByDescending(e => e.Date).ToListAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }

    [HttpPost("ap")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateAP([FromBody] APEntryEntity model)
    {
        model.Id = Guid.NewGuid();
        model.DocumentNumber = string.IsNullOrEmpty(model.DocumentNumber) ? $"AP-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..18] : model.DocumentNumber;
        if (model.Date != default) model.Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Utc);
        _db.APEntries.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = model.Id, documentNumber = model.DocumentNumber, tenantId = _tenant.TenantId });
    }

    [HttpGet("ar")]
    public async Task<IActionResult> GetAR()
    {
        var items = await _db.AREntries.OrderByDescending(e => e.Date).ToListAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }

    [HttpPost("ar")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateAR([FromBody] AREntryEntity model)
    {
        model.Id = Guid.NewGuid();
        model.DocumentNumber = string.IsNullOrEmpty(model.DocumentNumber) ? $"AR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..18] : model.DocumentNumber;
        if (model.Date != default) model.Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Utc);
        _db.AREntries.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = model.Id, documentNumber = model.DocumentNumber, tenantId = _tenant.TenantId });
    }

    [HttpGet("fixed-assets")]
    public async Task<IActionResult> GetFixedAssets()
    {
        var items = await _db.FixedAssets.OrderByDescending(a => a.PurchaseDate).ToListAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }

    [HttpPost("fixed-assets")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateFixedAsset([FromBody] FixedAssetEntity model)
    {
        model.Id = Guid.NewGuid();
        model.AssetCode = string.IsNullOrEmpty(model.AssetCode) ? $"FA-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..18] : model.AssetCode;
        if (model.PurchaseDate != default) model.PurchaseDate = DateTime.SpecifyKind(model.PurchaseDate, DateTimeKind.Utc);
        _db.FixedAssets.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = model.Id, assetCode = model.AssetCode, tenantId = _tenant.TenantId });
    }

    [HttpPost("ap/{id:guid}/pay")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PayAP(Guid id)
    {
        var entry = await _db.APEntries.FindAsync(id);
        if (entry == null) return NotFound();
        if (entry.Status == "Paid") return BadRequest(new { error = "Entry is already paid" });

        entry.Status = "Paid";
        entry.PaidAmount = entry.Amount;
        entry.UpdatedAt = DateTime.UtcNow;

        await _accounting.PostJournalEntryAsync(new JournalPostingRequest
        {
            DocumentNumber = entry.DocumentNumber,
            EntryDate = DateTime.UtcNow,
            Reference = "F-03",
            Description = $"AP payment to {entry.VendorName}",
            Lines = new List<JournalLine>
            {
                new() { AccountCode = "2000", Debit = entry.Amount },
                new() { AccountCode = "1010", Credit = entry.Amount }
            }
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = entry.Id, status = entry.Status, amount = entry.Amount, tenantId = _tenant.TenantId });
    }

    [HttpPost("ar/{id:guid}/pay")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PayAR(Guid id)
    {
        var entry = await _db.AREntries.FindAsync(id);
        if (entry == null) return NotFound();
        if (entry.Status == "Paid") return BadRequest(new { error = "Entry is already paid" });

        entry.Status = "Paid";
        entry.ReceivedAmount = entry.Amount;
        entry.UpdatedAt = DateTime.UtcNow;

        await _accounting.PostJournalEntryAsync(new JournalPostingRequest
        {
            DocumentNumber = entry.DocumentNumber,
            EntryDate = DateTime.UtcNow,
            Reference = "F-28",
            Description = $"AR payment received from {entry.CustomerName}",
            Lines = new List<JournalLine>
            {
                new() { AccountCode = "1010", Debit = entry.Amount },
                new() { AccountCode = "1100", Credit = entry.Amount }
            }
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = entry.Id, status = entry.Status, amount = entry.Amount, tenantId = _tenant.TenantId });
    }
}
