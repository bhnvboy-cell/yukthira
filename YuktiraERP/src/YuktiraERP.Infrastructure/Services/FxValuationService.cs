using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class FxValuationService : IFxValuationService
{
    private readonly YuktiraDbContext _db;

    public FxValuationService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<List<FxValuationItemDto>> PreviewRevaluationAsync(Guid tenantId, string baseCurrency, DateTime valuationDate)
    {
        var targetBase = string.IsNullOrWhiteSpace(baseCurrency) ? "INR" : baseCurrency.ToUpperInvariant();

        // Fetch foreign currency journal entries
        var foreignEntries = await _db.UniversalJournals
            .Where(j => j.TenantId == tenantId && !j.IsReversal && j.Currency != targetBase)
            .OrderByDescending(j => j.PostingDate)
            .Take(200)
            .ToListAsync();

        var rates = await _db.ExchangeRates
            .Where(r => r.TenantId == tenantId && r.ToCurrency == targetBase && r.EffectiveFrom <= valuationDate)
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync();

        var items = new List<FxValuationItemDto>();

        foreach (var entry in foreignEntries)
        {
            var closingRate = rates
                .FirstOrDefault(r => string.Equals(r.FromCurrency, entry.Currency, StringComparison.OrdinalIgnoreCase))?
                .Rate ?? entry.ExchangeRate;

            if (closingRate <= 0) closingRate = entry.ExchangeRate > 0 ? entry.ExchangeRate : 1.0m;

            var foreignAmt = entry.DebitAmount > 0 ? entry.DebitAmount : -entry.CreditAmount;
            var bookVal = entry.AmountLC != 0 ? entry.AmountLC : Math.Round(foreignAmt * entry.ExchangeRate, 2);
            var revaluedVal = Math.Round(foreignAmt * closingRate, 2);
            var delta = revaluedVal - bookVal;

            items.Add(new FxValuationItemDto
            {
                JournalId = entry.Id,
                DocumentNumber = entry.DocumentNumber,
                AccountCode = entry.AccountCode,
                AccountName = entry.AccountName,
                ForeignCurrency = entry.Currency,
                ForeignAmount = foreignAmt,
                HistoricalRate = entry.ExchangeRate,
                BookValueLC = bookVal,
                ClosingRate = closingRate,
                RevaluedValueLC = revaluedVal,
                UnrealizedDeltaLC = delta,
                ValuationStatus = delta > 0 ? "Gain" : delta < 0 ? "Loss" : "Neutral"
            });
        }

        return items;
    }

    public async Task<FxValuationRunResult> ExecuteRevaluationAsync(Guid tenantId, string baseCurrency, DateTime valuationDate, bool postEntries, string userId)
    {
        var targetBase = string.IsNullOrWhiteSpace(baseCurrency) ? "INR" : baseCurrency.ToUpperInvariant();
        var items = await PreviewRevaluationAsync(tenantId, targetBase, valuationDate);

        var totalGain = items.Where(i => i.UnrealizedDeltaLC > 0).Sum(i => i.UnrealizedDeltaLC);
        var totalLoss = Math.Abs(items.Where(i => i.UnrealizedDeltaLC < 0).Sum(i => i.UnrealizedDeltaLC));
        var netImpact = totalGain - totalLoss;

        var result = new FxValuationRunResult
        {
            ValuationDate = valuationDate,
            BaseCurrency = targetBase,
            ItemsEvaluated = items.Count,
            TotalUnrealizedGain = totalGain,
            TotalUnrealizedLoss = totalLoss,
            NetUnrealizedImpact = netImpact,
            Items = items,
            EntriesPosted = false
        };

        if (postEntries && netImpact != 0)
        {
            var docNum = "FX-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            result.JournalDocumentNumber = docNum;

            if (netImpact > 0)
            {
                // Unrealized Gain: Debit Balance Sheet Asset / Credit P&L Gain
                _db.UniversalJournals.Add(new UniversalJournalEntity
                {
                    TenantId = tenantId,
                    FiscalYear = valuationDate.Year,
                    Period = valuationDate.Month,
                    DocumentNumber = docNum,
                    DocumentType = "SA",
                    DocumentDate = valuationDate,
                    PostingDate = valuationDate,
                    LineNumber = 1,
                    AccountCode = "140800",
                    AccountName = "Unrealized FX Valuation Asset Reserve",
                    AccountType = "Asset",
                    DebitAmount = netImpact,
                    CreditAmount = 0,
                    Currency = targetBase,
                    ExchangeRate = 1.0m,
                    AmountLC = netImpact,
                    CreatedAt = DateTime.UtcNow
                });

                _db.UniversalJournals.Add(new UniversalJournalEntity
                {
                    TenantId = tenantId,
                    FiscalYear = valuationDate.Year,
                    Period = valuationDate.Month,
                    DocumentNumber = docNum,
                    DocumentType = "SA",
                    DocumentDate = valuationDate,
                    PostingDate = valuationDate,
                    LineNumber = 2,
                    AccountCode = "800100",
                    AccountName = "Unrealized Foreign Exchange Gain",
                    AccountType = "Revenue",
                    DebitAmount = 0,
                    CreditAmount = netImpact,
                    Currency = targetBase,
                    ExchangeRate = 1.0m,
                    AmountLC = netImpact,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                var absLoss = Math.Abs(netImpact);
                // Unrealized Loss: Debit P&L Loss / Credit Balance Sheet Liability
                _db.UniversalJournals.Add(new UniversalJournalEntity
                {
                    TenantId = tenantId,
                    FiscalYear = valuationDate.Year,
                    Period = valuationDate.Month,
                    DocumentNumber = docNum,
                    DocumentType = "SA",
                    DocumentDate = valuationDate,
                    PostingDate = valuationDate,
                    LineNumber = 1,
                    AccountCode = "800200",
                    AccountName = "Unrealized Foreign Exchange Loss",
                    AccountType = "Expense",
                    DebitAmount = absLoss,
                    CreditAmount = 0,
                    Currency = targetBase,
                    ExchangeRate = 1.0m,
                    AmountLC = absLoss,
                    CreatedAt = DateTime.UtcNow
                });

                _db.UniversalJournals.Add(new UniversalJournalEntity
                {
                    TenantId = tenantId,
                    FiscalYear = valuationDate.Year,
                    Period = valuationDate.Month,
                    DocumentNumber = docNum,
                    DocumentType = "SA",
                    DocumentDate = valuationDate,
                    PostingDate = valuationDate,
                    LineNumber = 2,
                    AccountCode = "240800",
                    AccountName = "Unrealized FX Valuation Liability Reserve",
                    AccountType = "Liability",
                    DebitAmount = 0,
                    CreditAmount = absLoss,
                    Currency = targetBase,
                    ExchangeRate = 1.0m,
                    AmountLC = absLoss,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            result.EntriesPosted = true;
        }

        return result;
    }
}
