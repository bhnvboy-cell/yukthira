using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class AccountingService : IAccountingService
{
    private readonly YuktiraDbContext _db;

    public AccountingService(YuktiraDbContext db) { _db = db; }

    public async Task PostJournalEntryAsync(JournalPostingRequest request)
    {
        if (request.Lines.Count < 2)
            throw new InvalidOperationException("Journal entry must have at least 2 lines");

        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);
        if (totalDebit != totalCredit)
            throw new InvalidOperationException($"Debit ({totalDebit}) must equal Credit ({totalCredit})");

        var period = $"{request.EntryDate.Year}-{request.EntryDate.Month:D2}";
        var docNum = string.IsNullOrEmpty(request.DocumentNumber)
            ? $"GL-{request.EntryDate:yyyyMMdd}-{Guid.NewGuid():N}"[..20]
            : request.DocumentNumber;

        foreach (var line in request.Lines)
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountCode == line.AccountCode);
            if (account == null)
                throw new InvalidOperationException($"Account not found: {line.AccountCode}");

            account.Balance += line.Debit - line.Credit;

            _db.GeneralLedgerEntries.Add(new GeneralLedgerEntryEntity
            {
                DocumentNumber = docNum,
                EntryDate = request.EntryDate,
                AccountCode = line.AccountCode,
                AccountName = account.AccountName,
                Debit = line.Debit,
                Credit = line.Credit,
                Reference = request.Reference,
                Description = request.Description,
                Period = period,
                IsPosted = true
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<TrialBalanceDto>> GetTrialBalanceAsync(Guid tenantId, DateTime? asOfDate = null)
    {
        var query = _db.GeneralLedgerEntries.Where(g => g.TenantId == tenantId);
        if (asOfDate.HasValue)
            query = query.Where(g => g.EntryDate <= asOfDate.Value);

        var entries = await query.ToListAsync();
        var accounts = await _db.Accounts.Where(a => a.IsActive).ToListAsync();

        return accounts.Select(a =>
        {
            var accountEntries = entries.Where(e => e.AccountCode == a.AccountCode).ToList();
            var totalDebit = accountEntries.Sum(e => e.Debit);
            var totalCredit = accountEntries.Sum(e => e.Credit);
            var balance = totalDebit - totalCredit;

            return new TrialBalanceDto
            {
                AccountCode = a.AccountCode,
                AccountName = a.AccountName,
                Type = a.Type,
                DebitBalance = balance > 0 ? balance : 0,
                CreditBalance = balance < 0 ? -balance : 0,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit
            };
        }).ToList();
    }

    public async Task<List<TrialBalanceDto>> GetProfitAndLossAsync(Guid tenantId, DateTime fromDate, DateTime toDate)
    {
        var entries = await _db.GeneralLedgerEntries
            .Where(g => g.TenantId == tenantId && g.EntryDate >= fromDate && g.EntryDate <= toDate)
            .ToListAsync();

        var allAccounts = await _db.Accounts.Where(a => a.IsActive).ToListAsync();
        var incomeExpenseAccounts = allAccounts
            .Where(a => a.Type == "Income" || a.Type == "Expense")
            .ToList();

        return incomeExpenseAccounts.Select(a =>
        {
            var accountEntries = entries.Where(e => e.AccountCode == a.AccountCode).ToList();
            var totalDebit = accountEntries.Sum(e => e.Debit);
            var totalCredit = accountEntries.Sum(e => e.Credit);
            var balance = totalDebit - totalCredit;

            return new TrialBalanceDto
            {
                AccountCode = a.AccountCode,
                AccountName = a.AccountName,
                Type = a.Type,
                DebitBalance = balance > 0 ? balance : 0,
                CreditBalance = balance < 0 ? -balance : 0,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit
            };
        }).ToList();
    }

    public async Task<List<TrialBalanceDto>> GetBalanceSheetAsync(Guid tenantId, DateTime asOfDate)
    {
        var entries = await _db.GeneralLedgerEntries
            .Where(g => g.TenantId == tenantId && g.EntryDate <= asOfDate)
            .ToListAsync();

        var allAccountsBs = await _db.Accounts.Where(a => a.IsActive).ToListAsync();
        var bsAccounts = allAccountsBs
            .Where(a => a.Type == "Asset" || a.Type == "Liability" || a.Type == "Equity")
            .ToList();

        return bsAccounts.Select(a =>
        {
            var accountEntries = entries.Where(e => e.AccountCode == a.AccountCode).ToList();
            var totalDebit = accountEntries.Sum(e => e.Debit);
            var totalCredit = accountEntries.Sum(e => e.Credit);
            var balance = totalDebit - totalCredit;

            return new TrialBalanceDto
            {
                AccountCode = a.AccountCode,
                AccountName = a.AccountName,
                Type = a.Type,
                DebitBalance = balance > 0 ? balance : 0,
                CreditBalance = balance < 0 ? -balance : 0,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit
            };
        }).ToList();
    }

    // ── AP/AR Aging ──
    public async Task<AgingSummaryDto> GetAccountsPayableAgingAsync(Guid tenantId, DateTime asOfDate)
    {
        var entries = await _db.APEntries
            .Where(a => a.TenantId == tenantId && a.Status != "Paid" && a.Status != "Closed")
            .ToListAsync();
        return ComputeAging(entries.Select(e => (Party: e.VendorName, Date: e.Date, Open: e.Amount - e.PaidAmount)).ToList(), "AP", asOfDate);
    }

    public async Task<AgingSummaryDto> GetAccountsReceivableAgingAsync(Guid tenantId, DateTime asOfDate)
    {
        var entries = await _db.AREntries
            .Where(a => a.TenantId == tenantId && a.Status != "Paid" && a.Status != "Closed")
            .ToListAsync();
        return ComputeAging(entries.Select(e => (Party: e.CustomerName, Date: e.Date, Open: e.Amount - e.ReceivedAmount)).ToList(), "AR", asOfDate);
    }

    private static AgingSummaryDto ComputeAging(List<(string Party, DateTime Date, decimal Open)> items, string kind, DateTime asOfDate)
    {
        var summary = new AgingSummaryDto { Kind = kind };
        var grouped = items.GroupBy(i => i.Party).OrderBy(g => g.Key);
        foreach (var g in grouped)
        {
            var bucket = new AgingBucketDto { PartyName = g.Key };
            foreach (var (_, date, open) in g)
            {
                var days = Math.Max(0, (asOfDate - date).Days);
                if (days > 90) bucket.Over90 += open;
                else if (days > 60) bucket.Days61to90 += open;
                else if (days > 30) bucket.Days31to60 += open;
                else if (days > 0) bucket.Days1to30 += open;
                else bucket.Current += open;
            }
            bucket.Total = bucket.Current + bucket.Days1to30 + bucket.Days31to60 + bucket.Days61to90 + bucket.Over90;
            summary.Parties.Add(bucket);
            summary.Current += bucket.Current;
            summary.Days1to30 += bucket.Days1to30;
            summary.Days31to60 += bucket.Days31to60;
            summary.Days61to90 += bucket.Days61to90;
            summary.Over90 += bucket.Over90;
        }
        summary.TotalOutstanding = summary.Parties.Sum(p => p.Total);
        return summary;
    }

    // ── Payments ──
    public async Task PostPaymentAsync(Guid tenantId, PaymentRequest request)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be positive");

        var payment = new PaymentEntity
        {
            TenantId = tenantId,
            PaymentNumber = $"{(request.Type == "Receipt" ? "RCP" : "PAY")}-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}"[..20],
            Date = request.Date,
            PartyName = request.PartyName,
            Type = request.Type,
            Reference = request.Reference,
            Amount = request.Amount,
            Method = request.Method,
            Status = "Posted"
        };
        _db.Payments.Add(payment);

        // Settle the oldest open AP/AR entry for this party (FIFO)
        if (request.Type == "Payment")
        {
            var entry = await _db.APEntries
                .Where(a => a.TenantId == tenantId && a.VendorName == request.PartyName && a.Status == "Open")
                .OrderBy(a => a.Date)
                .FirstOrDefaultAsync();
            if (entry != null)
            {
                entry.PaidAmount += request.Amount;
                if (entry.PaidAmount >= entry.Amount) entry.Status = "Paid";
            }
        }
        else
        {
            var entry = await _db.AREntries
                .Where(a => a.TenantId == tenantId && a.CustomerName == request.PartyName && a.Status == "Open")
                .OrderBy(a => a.Date)
                .FirstOrDefaultAsync();
            if (entry != null)
            {
                entry.ReceivedAmount += request.Amount;
                if (entry.ReceivedAmount >= entry.Amount) entry.Status = "Paid";
            }
        }

        // Post the GL effect
        var cashAccount = await _db.Accounts.FirstOrDefaultAsync(a =>
            string.IsNullOrEmpty(request.CashAccountCode)
                ? a.Type == "Asset" && a.Category == "Current" && a.AccountName.ToLower().Contains("bank")
                : a.AccountCode == request.CashAccountCode);
        var cashCode = cashAccount?.AccountCode ?? "BANK";
        if (cashAccount == null && !_db.Accounts.Any(a => a.AccountCode == "BANK"))
        {
            _db.Accounts.Add(new AccountEntity { AccountCode = "BANK", AccountName = "Bank", Type = "Asset", Category = "Current" });
            cashCode = "BANK";
        }

        var counterAccount = request.Type == "Payment"
            ? await _db.Accounts.FirstOrDefaultAsync(a => a.Type == "Liability" && a.Category == "Current")
            : await _db.Accounts.FirstOrDefaultAsync(a => a.Type == "Asset" && a.Category == "Current" && a.AccountCode != cashCode);
        var counterCode = counterAccount?.AccountCode ?? (request.Type == "Payment" ? "AP" : "AR");
        if (counterAccount == null && !_db.Accounts.Any(a => a.AccountCode == counterCode))
        {
            _db.Accounts.Add(new AccountEntity { AccountCode = counterCode, AccountName = request.Type == "Payment" ? "Accounts Payable" : "Accounts Receivable", Type = request.Type == "Payment" ? "Liability" : "Asset", Category = "Current" });
        }

        var period = $"{request.Date.Year}-{request.Date.Month:D2}";
        if (request.Type == "Payment")
        {
            _db.GeneralLedgerEntries.Add(new GeneralLedgerEntryEntity { TenantId = tenantId, DocumentNumber = payment.PaymentNumber, EntryDate = request.Date, AccountCode = counterCode, AccountName = "Accounts Payable", Debit = request.Amount, Credit = 0, Reference = payment.PaymentNumber, Description = $"Payment to {request.PartyName}", Period = period, IsPosted = true });
            _db.GeneralLedgerEntries.Add(new GeneralLedgerEntryEntity { TenantId = tenantId, DocumentNumber = payment.PaymentNumber, EntryDate = request.Date, AccountCode = cashCode, AccountName = cashAccount?.AccountName ?? "Bank", Debit = 0, Credit = request.Amount, Reference = payment.PaymentNumber, Description = $"Payment to {request.PartyName}", Period = period, IsPosted = true });
        }
        else
        {
            _db.GeneralLedgerEntries.Add(new GeneralLedgerEntryEntity { TenantId = tenantId, DocumentNumber = payment.PaymentNumber, EntryDate = request.Date, AccountCode = cashCode, AccountName = cashAccount?.AccountName ?? "Bank", Debit = request.Amount, Credit = 0, Reference = payment.PaymentNumber, Description = $"Receipt from {request.PartyName}", Period = period, IsPosted = true });
            _db.GeneralLedgerEntries.Add(new GeneralLedgerEntryEntity { TenantId = tenantId, DocumentNumber = payment.PaymentNumber, EntryDate = request.Date, AccountCode = counterCode, AccountName = "Accounts Receivable", Debit = 0, Credit = request.Amount, Reference = payment.PaymentNumber, Description = $"Receipt from {request.PartyName}", Period = period, IsPosted = true });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<PaymentRecordDto>> GetPaymentHistoryAsync(Guid tenantId, int limit = 50)
    {
        return await _db.Payments
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.Date)
            .Take(limit)
            .Select(p => new PaymentRecordDto
            {
                Id = p.Id,
                PaymentNumber = p.PaymentNumber,
                Date = p.Date,
                PartyName = p.PartyName,
                Type = p.Type,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status
            })
            .ToListAsync();
    }

    // ── Period Close ──
    public async Task OpenPeriodAsync(Guid tenantId, PeriodCloseRequest request)
    {
        var existing = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Period == request.Period);
        if (existing != null)
            throw new InvalidOperationException($"Period {request.Period} already exists");

        _db.FiscalPeriods.Add(new FiscalPeriodEntity
        {
            TenantId = tenantId,
            Period = request.Period,
            FiscalYear = request.FiscalYear,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = "Open"
        });
        await _db.SaveChangesAsync();
    }

    public async Task ClosePeriodAsync(Guid tenantId, string period, string closedBy)
    {
        var fiscal = await _db.FiscalPeriods
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Period == period);
        if (fiscal == null)
            throw new InvalidOperationException($"Period {period} not found. Open it first.");
        if (fiscal.Status == "Closed")
            throw new InvalidOperationException($"Period {period} is already closed");

        // Prevent new postings in a closed period
        var lateEntries = await _db.GeneralLedgerEntries
            .AnyAsync(g => g.TenantId == tenantId && g.Period == period && !g.IsPosted);
        fiscal.Status = "Closed";
        fiscal.ClosedAt = DateTime.UtcNow;
        fiscal.ClosedBy = closedBy;
        await _db.SaveChangesAsync();
        if (lateEntries)
            throw new InvalidOperationException($"Period {period} closed but there are unposted GL entries for it");
    }

    public async Task<List<FiscalPeriodDto>> GetFiscalPeriodsAsync(Guid tenantId)
    {
        return await _db.FiscalPeriods
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new FiscalPeriodDto
            {
                Id = p.Id,
                Period = p.Period,
                FiscalYear = p.FiscalYear,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status,
                ClosedAt = p.ClosedAt
            })
            .ToListAsync();
    }

    // ── Bank Reconciliation ──
    public async Task PostBankReconciliationAsync(Guid tenantId, BankReconciliationRequest request)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountCode == request.AccountCode);
        if (account == null)
            throw new InvalidOperationException($"Account not found: {request.AccountCode}");

        _db.BankReconciliations.Add(new BankReconciliationEntity
        {
            TenantId = tenantId,
            AccountCode = request.AccountCode,
            AccountName = account.AccountName,
            StatementDate = request.StatementDate,
            StatementBalance = request.StatementBalance,
            LedgerBalance = request.LedgerBalance,
            Difference = request.StatementBalance - request.LedgerBalance,
            Status = Math.Abs(request.StatementBalance - request.LedgerBalance) < 0.01m ? "Matched" : "OutOfBalance",
            Notes = request.Notes
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<BankReconciliationDto>> GetBankReconciliationsAsync(Guid tenantId, int limit = 50)
    {
        return await _db.BankReconciliations
            .Where(b => b.TenantId == tenantId)
            .OrderByDescending(b => b.StatementDate)
            .Take(limit)
            .Select(b => new BankReconciliationDto
            {
                Id = b.Id,
                AccountCode = b.AccountCode,
                AccountName = b.AccountName,
                StatementDate = b.StatementDate,
                StatementBalance = b.StatementBalance,
                LedgerBalance = b.LedgerBalance,
                Difference = b.Difference,
                Status = b.Status
            })
            .ToListAsync();
    }

    // ── Fixed Asset Depreciation (straight-line) ──
    public async Task RunDepreciationAsync(Guid tenantId, DepreciationRunRequest request)
    {
        var assets = await _db.FixedAssets.Where(a => a.Status == "Active").ToListAsync();
        if (assets.Count == 0)
            throw new InvalidOperationException("No active fixed assets to depreciate");

        var period = string.IsNullOrEmpty(request.Period)
            ? $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month:D2}"
            : request.Period;

        foreach (var asset in assets)
        {
            var existing = await _db.DepreciationSchedules
                .AnyAsync(d => d.TenantId == tenantId && d.AssetId == asset.Id && d.Period == period);
            if (existing) continue;

            var monthlyDep = asset.UsefulLifeYears > 0
                ? (asset.Cost - asset.SalvageValue) / (asset.UsefulLifeYears * 12)
                : 0;
            var priorAccumulated = (await _db.DepreciationSchedules
                .Where(d => d.TenantId == tenantId && d.AssetId == asset.Id)
                .SumAsync(d => (decimal?)d.DepreciationAmount)) ?? 0;
            var accumulated = priorAccumulated + monthlyDep;

            _db.DepreciationSchedules.Add(new DepreciationScheduleEntity
            {
                TenantId = tenantId,
                AssetId = asset.Id,
                AssetCode = asset.AssetCode,
                AssetName = asset.AssetName,
                Period = period,
                DepreciationAmount = monthlyDep,
                AccumulatedDepreciation = accumulated,
                BookValue = Math.Max(asset.SalvageValue, asset.Cost - accumulated),
                Status = "Posted"
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<DepreciationScheduleDto>> GetDepreciationScheduleAsync(Guid tenantId, string? period = null)
    {
        var query = _db.DepreciationSchedules.Where(d => d.TenantId == tenantId);
        if (!string.IsNullOrEmpty(period))
            query = query.Where(d => d.Period == period);

        return await query
            .OrderByDescending(d => d.Period)
            .Select(d => new DepreciationScheduleDto
            {
                Id = d.Id,
                AssetCode = d.AssetCode,
                AssetName = d.AssetName,
                Period = d.Period,
                DepreciationAmount = d.DepreciationAmount,
                AccumulatedDepreciation = d.AccumulatedDepreciation,
                BookValue = d.BookValue,
                Status = d.Status
            })
            .ToListAsync();
    }
}