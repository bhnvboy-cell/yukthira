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
            .Where(e => e.TenantId == _tenant.TenantId)
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
        var result = await _accounting.GetTrialBalanceAsync(_tenant.TenantId, asOfDate);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("profit-loss")]
    public async Task<IActionResult> ProfitLoss([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.Today.AddMonths(-1);
        var toDate = to ?? DateTime.Today;
        var result = await _accounting.GetProfitAndLossAsync(_tenant.TenantId, fromDate, toDate);
        return Ok(new { data = result, fromDate, toDate, tenantId = _tenant.TenantId });
    }

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet([FromQuery] DateTime? asOfDate)
    {
        var date = asOfDate ?? DateTime.Today;
        var result = await _accounting.GetBalanceSheetAsync(_tenant.TenantId, date);
        return Ok(new { data = result, asOfDate = date, tenantId = _tenant.TenantId });
    }

    [HttpGet("ap")]
    public async Task<IActionResult> GetAP()
    {
        var items = await _db.APEntries.Where(e => e.TenantId == _tenant.TenantId).OrderByDescending(e => e.Date).ToListAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }

    [HttpPost("ap")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateAP([FromBody] APEntryEntity model)
    {
        model.Id = Guid.NewGuid();
        model.TenantId = _tenant.TenantId;
        model.DocumentNumber = string.IsNullOrEmpty(model.DocumentNumber) ? $"AP-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..18] : model.DocumentNumber;
        if (model.Date != default) model.Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Utc);
        _db.APEntries.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = model.Id, documentNumber = model.DocumentNumber, tenantId = _tenant.TenantId });
    }

    [HttpGet("ar")]
    public async Task<IActionResult> GetAR()
    {
        var items = await _db.AREntries.Where(e => e.TenantId == _tenant.TenantId).OrderByDescending(e => e.Date).ToListAsync();
        return Ok(new { data = items, tenantId = _tenant.TenantId });
    }

    [HttpPost("ar")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> CreateAR([FromBody] AREntryEntity model)
    {
        model.Id = Guid.NewGuid();
        model.TenantId = _tenant.TenantId;
        model.DocumentNumber = string.IsNullOrEmpty(model.DocumentNumber) ? $"AR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..18] : model.DocumentNumber;
        if (model.Date != default) model.Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Utc);
        _db.AREntries.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = model.Id, documentNumber = model.DocumentNumber, tenantId = _tenant.TenantId });
    }

    // ── Finance loop endpoints: AP/AR aging ──
    [HttpGet("aging/ap")]
    public async Task<IActionResult> ApAging([FromQuery] DateTime? asOf)
    {
        var result = await _accounting.GetAccountsPayableAgingAsync(_tenant.TenantId, asOf ?? DateTime.Today);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    [HttpGet("aging/ar")]
    public async Task<IActionResult> ArAging([FromQuery] DateTime? asOf)
    {
        var result = await _accounting.GetAccountsReceivableAgingAsync(_tenant.TenantId, asOf ?? DateTime.Today);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    // ── Payments ──
    [HttpPost("payments")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PostPayment([FromBody] PaymentRequest request)
    {
        try
        {
            await _accounting.PostPaymentAsync(_tenant.TenantId, request);
            return Ok(new { success = true, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] int limit = 50)
    {
        var result = await _accounting.GetPaymentHistoryAsync(_tenant.TenantId, limit);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    // ── Period close ──
    [HttpPost("periods/open")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> OpenPeriod([FromBody] PeriodCloseRequest request)
    {
        try
        {
            await _accounting.OpenPeriodAsync(_tenant.TenantId, request);
            return Ok(new { success = true, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("periods/{period}/close")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> ClosePeriod(string period)
    {
        try
        {
            await _accounting.ClosePeriodAsync(_tenant.TenantId, period, User.Identity?.Name ?? "");
            return Ok(new { success = true, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods()
    {
        var result = await _accounting.GetFiscalPeriodsAsync(_tenant.TenantId);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    // ── Bank reconciliation ──
    [HttpPost("bank-reconciliation")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> PostBankReconciliation([FromBody] BankReconciliationRequest request)
    {
        try
        {
            await _accounting.PostBankReconciliationAsync(_tenant.TenantId, request);
            return Ok(new { success = true, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("bank-reconciliation")]
    public async Task<IActionResult> GetBankReconciliations([FromQuery] int limit = 50)
    {
        var result = await _accounting.GetBankReconciliationsAsync(_tenant.TenantId, limit);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
    }

    // ── Depreciation ──
    [HttpPost("depreciation/run")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> RunDepreciation([FromBody] DepreciationRunRequest request)
    {
        try
        {
            await _accounting.RunDepreciationAsync(_tenant.TenantId, request);
            return Ok(new { success = true, tenantId = _tenant.TenantId });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("depreciation")]
    public async Task<IActionResult> GetDepreciation([FromQuery] string? period = null)
    {
        var result = await _accounting.GetDepreciationScheduleAsync(_tenant.TenantId, period);
        return Ok(new { data = result, tenantId = _tenant.TenantId });
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
        try { model.ValidateLifecycle(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        _db.FixedAssets.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = model.Id, assetCode = model.AssetCode, tenantId = _tenant.TenantId });
    }

    [HttpGet("fixed-assets/{id:guid}")]
    public async Task<IActionResult> GetFixedAsset(Guid id)
    {
        var item = await _db.FixedAssets.FirstOrDefaultAsync(a => a.Id == id);
        return item == null ? NotFound() : Ok(new { data = item, tenantId = _tenant.TenantId });
    }

    [HttpPut("fixed-assets/{id:guid}")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> UpdateFixedAsset(Guid id, [FromBody] FixedAssetEntity model)
    {
        var item = await _db.FixedAssets.FirstOrDefaultAsync(a => a.Id == id);
        if (item == null) return NotFound();
        try { model.ValidateLifecycle(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }

        item.AssetName = model.AssetName;
        item.Category = model.Category;
        item.Cost = model.Cost;
        item.SalvageValue = model.SalvageValue;
        item.UsefulLifeYears = model.UsefulLifeYears;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = item.Id, tenantId = _tenant.TenantId });
    }

    [HttpPost("fixed-assets/{id:guid}/dispose")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> DisposeFixedAsset(Guid id)
    {
        var item = await _db.FixedAssets.FirstOrDefaultAsync(a => a.Id == id);
        if (item == null) return NotFound();
        if (item.Status != "Active") return BadRequest(new { error = $"Asset is {item.Status}; only Active assets can be disposed" });

        var bookValue = item.BookValue(DateTime.UtcNow);
        item.MarkScrapped();
        item.UpdatedAt = DateTime.UtcNow;

        await _accounting.PostJournalEntryAsync(new JournalPostingRequest
        {
            DocumentNumber = item.AssetCode,
            EntryDate = DateTime.UtcNow,
            Reference = "F-ASSET-DISPOSE",
            Description = $"Disposal of fixed asset {item.AssetCode} ({item.AssetName}) at book value",
            Lines = new List<JournalLine>
            {
                new() { AccountCode = "1400", Debit = bookValue },
                new() { AccountCode = "1300", Credit = bookValue }
            }
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = item.Id, status = item.Status, bookValue, tenantId = _tenant.TenantId });
    }

    [HttpPost("fixed-assets/{id:guid}/transfer")]
    [Authorize(Policy = "PowerUserOrAbove")]
    public async Task<IActionResult> TransferFixedAsset(Guid id, [FromBody] AssetTransferRequest request)
    {
        var item = await _db.FixedAssets.FirstOrDefaultAsync(a => a.Id == id);
        if (item == null) return NotFound();
        if (item.Status == "Scrapped") return BadRequest(new { error = "Scrapped assets cannot be transferred" });
        if (string.IsNullOrWhiteSpace(request.ToDepartment)) return BadRequest(new { error = "Target department is required" });

        var bookValue = item.BookValue(DateTime.UtcNow);
        item.MarkTransferred();
        item.UpdatedAt = DateTime.UtcNow;

        await _accounting.PostJournalEntryAsync(new JournalPostingRequest
        {
            DocumentNumber = item.AssetCode,
            EntryDate = DateTime.UtcNow,
            Reference = "F-ASSET-TRANSFER",
            Description = $"Transfer of fixed asset {item.AssetCode} to {request.ToDepartment} (book value)",
            Lines = new List<JournalLine>
            {
                new() { AccountCode = "1300", Debit = bookValue },
                new() { AccountCode = "1300", Credit = bookValue }
            }
        });

        await _db.SaveChangesAsync();
        return Ok(new { success = true, id = item.Id, status = item.Status, toDepartment = request.ToDepartment, bookValue, tenantId = _tenant.TenantId });
    }

    public class AssetTransferRequest { public string ToDepartment { get; set; } = ""; }

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

        try { entry.ApplyReceipt(entry.Amount); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        entry.Status = "Paid";
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
