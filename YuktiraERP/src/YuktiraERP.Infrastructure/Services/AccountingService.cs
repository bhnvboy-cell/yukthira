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

    public async Task<List<TrialBalanceDto>> GetTrialBalanceAsync(DateTime? asOfDate = null)
    {
        var query = _db.GeneralLedgerEntries.AsQueryable();
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

    public async Task<List<TrialBalanceDto>> GetProfitAndLossAsync(DateTime fromDate, DateTime toDate)
    {
        var entries = await _db.GeneralLedgerEntries
            .Where(g => g.EntryDate >= fromDate && g.EntryDate <= toDate)
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

    public async Task<List<TrialBalanceDto>> GetBalanceSheetAsync(DateTime asOfDate)
    {
        var entries = await _db.GeneralLedgerEntries
            .Where(g => g.EntryDate <= asOfDate)
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
}
