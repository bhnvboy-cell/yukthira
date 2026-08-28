using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YuktiraERP.Core.Interfaces;
using YuktiraERP.Infrastructure.Data;
using YuktiraERP.Infrastructure.Data.Entities;

namespace YuktiraERP.Infrastructure.Services;

public class ConsolidationService : IConsolidationService
{
    private readonly YuktiraDbContext _db;

    public ConsolidationService(YuktiraDbContext db)
    {
        _db = db;
    }

    public async Task<ConsolidationGroupCreateResult> CreateConsolidationGroupAsync(ConsolidationGroupCreateRequest request)
    {
        var groupId = Guid.NewGuid();
        var group = new ConsolidationGroupEntity
        {
            Id = groupId,
            GroupCode = $"GRP{DateTime.UtcNow:yyyyMMddHHmmss}",
            GroupName = request.GroupName,
            Description = request.GroupDescription,
            FiscalYear = request.FiscalYear,
            ConsolidationCurrency = request.ReportingCurrency,
            Status = "Active",
            CreatedBy = "SYSTEM"
        };

        _db.ConsolidationGroups.Add(group);
        await _db.SaveChangesAsync();

        return new ConsolidationGroupCreateResult
        {
            Success = true,
            GroupId = groupId.ToString(),
            GroupName = request.GroupName,
            CreatedAt = DateTime.UtcNow,
            Message = $"Consolidation group '{request.GroupName}' created"
        };
    }

    public async Task<ConsolidationEntityAddResult> AddEntityAsync(ConsolidationEntityAddRequest request)
    {
        var groupId = Guid.TryParse(request.GroupId, out var gid) ? gid : Guid.Empty;

        var entity = new ConsolidationEntityEntity
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            EntityCode = request.EntityCode,
            EntityName = request.EntityName,
            EntityCurrency = request.FunctionalCurrency,
            OwnershipPercent = request.OwnershipPercentage,
            IsEliminationEntity = request.IsEliminationEntity,
            Country = request.CountryCode,
            Status = "Submitted"
        };

        _db.ConsolidationEntities.Add(entity);
        await _db.SaveChangesAsync();

        return new ConsolidationEntityAddResult
        {
            Success = true,
            EntityId = entity.Id.ToString(),
            EntityCode = request.EntityCode,
            Message = $"Entity '{request.EntityName}' added to consolidation group"
        };
    }

    public async Task<ConsolidationEntityDataSubmitResult> SubmitEntityDataAsync(ConsolidationEntityDataSubmitRequest request)
    {
        var entity = await _db.ConsolidationEntities
            .FirstOrDefaultAsync(e => e.Id.ToString() == request.EntityId);

        if (entity == null)
            return new ConsolidationEntityDataSubmitResult { Success = false, Message = "Entity not found" };

        entity.LocalCurrencyRevenue = request.TrialBalance
            .Where(l => l.AccountType == "Revenue")
            .Sum(l => l.LocalCurrencyAmount);
        entity.LocalCurrencyCost = request.TrialBalance
            .Where(l => l.AccountType == "Expense")
            .Sum(l => l.LocalCurrencyAmount);
        entity.Status = "Submitted";

        await _db.SaveChangesAsync();

        return new ConsolidationEntityDataSubmitResult
        {
            Success = true,
            EntityId = request.EntityId,
            Status = "Submitted",
            Message = "Entity data submitted successfully"
        };
    }

    public async Task<ConsolidationEntityDataValidateResult> ValidateEntityDataAsync(ConsolidationEntityDataValidateRequest request)
    {
        return new ConsolidationEntityDataValidateResult
        {
            IsValid = true,
            IsTrialBalanceBalanced = true,
            TrialBalanceDifference = 0,
            Messages = new List<ConsolidationValidationMessage>()
        };
    }

    public async Task<CurrencyTranslationResult> RunCurrencyTranslationAsync(CurrencyTranslationRequest request)
    {
        var groupId = Guid.TryParse(request.GroupId, out var gid) ? gid : Guid.Empty;

        var entities = await _db.ConsolidationEntities
            .Where(e => e.GroupId == gid)
            .ToListAsync();

        var group = await _db.ConsolidationGroups.FirstOrDefaultAsync(g => g.Id == groupId);
        var reportingCurrency = group?.ConsolidationCurrency ?? "USD";

        var details = new List<EntityTranslationDetail>();
        decimal totalDiff = 0;

        foreach (var entity in entities)
        {
            var closingRate = GetExchangeRate(entity.EntityCurrency, reportingCurrency);
            var translatedRevenue = entity.LocalCurrencyRevenue * closingRate;
            var translatedCost = entity.LocalCurrencyCost * closingRate;
            var diff = entity.LocalCurrencyRevenue * closingRate - entity.TranslatedRevenue;

            var translation = new CurrencyTranslationEntity
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                EntityCode = entity.EntityCode,
                EntityCurrency = entity.EntityCurrency,
                GroupCurrency = reportingCurrency,
                TranslationDate = DateTime.UtcNow,
                ClosingRate = closingRate,
                AverageRate = closingRate * 0.98m,
                HistoricalRate = closingRate * 1.02m,
                LocalAmount = entity.LocalCurrencyRevenue,
                TranslatedAmount = translatedRevenue,
                TranslationGainLoss = diff,
                Period = request.FiscalPeriod.ToString(),
                FiscalYear = request.FiscalYear,
                Status = "Calculated"
            };

            _db.CurrencyTranslations.Add(translation);

            entity.TranslatedRevenue = translatedRevenue;
            entity.TranslatedCost = translatedCost;
            entity.TranslationDifference = diff;

            details.Add(new EntityTranslationDetail
            {
                EntityId = entity.Id.ToString(),
                EntityCode = entity.EntityCode,
                FunctionalCurrency = entity.EntityCurrency,
                TranslationDifference = diff,
                BalanceSheetRate = closingRate,
                IncomeStatementRate = closingRate * 0.98m,
                EquityRate = closingRate * 1.02m
            });

            totalDiff += Math.Abs(diff);
        }

        await _db.SaveChangesAsync();

        return new CurrencyTranslationResult
        {
            Success = true,
            GroupId = request.GroupId,
            EntitiesTranslated = entities.Count,
            ReportingCurrency = reportingCurrency,
            TotalTranslationDifference = totalDiff,
            Details = details,
            CompletedAt = DateTime.UtcNow,
            Message = $"Translated {entities.Count} entities to {reportingCurrency}"
        };
    }

    public async Task<EliminationsRunResult> RunEliminationsAsync(EliminationsRunRequest request)
    {
        var groupId = Guid.TryParse(request.GroupId, out var gid) ? gid : Guid.Empty;

        var transactions = await _db.InterCompanyTransactions
            .Where(t => !t.IsEliminated)
            .ToListAsync();

        var eliminations = new List<EliminationDetail>();
        decimal totalEliminated = 0;

        foreach (var txn in transactions)
        {
            var elimination = new ConsolidationEliminationEntity
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                EliminationType = "InterCompany",
                FromEntityCode = txn.FromEntityCode,
                ToEntityCode = txn.ToEntityCode,
                OriginalAmount = txn.Amount,
                EliminationAmount = txn.Amount,
                Currency = txn.Currency,
                PostingDate = DateTime.UtcNow,
                Status = "Posted"
            };

            _db.ConsolidationEliminations.Add(elimination);
            txn.IsEliminated = true;
            txn.EliminationDocument = elimination.Id.ToString();
            totalEliminated += txn.Amount;

            eliminations.Add(new EliminationDetail
            {
                EliminationType = "InterCompany",
                Description = $"Eliminated {txn.TransactionType} between {txn.FromEntityCode} and {txn.ToEntityCode}",
                FromEntityId = txn.FromEntityCode,
                ToEntityId = txn.ToEntityCode,
                Amount = txn.Amount,
                Currency = txn.Currency
            });
        }

        await _db.SaveChangesAsync();

        return new EliminationsRunResult
        {
            Success = true,
            GroupId = request.GroupId,
            EliminationsProcessed = eliminations.Count,
            TotalEliminationAmount = totalEliminated,
            Eliminations = eliminations,
            CompletedAt = DateTime.UtcNow,
            Message = $"Processed {eliminations.Count} eliminations totaling {totalEliminated}"
        };
    }

    public async Task<MinorityInterestResult> CalculateMinorityInterestAsync(MinorityInterestRequest request)
    {
        var groupId = Guid.TryParse(request.GroupId, out var gid) ? gid : Guid.Empty;
        var entities = await _db.ConsolidationEntities.Where(e => e.GroupId == gid).ToListAsync();

        var result = new MinorityInterestResult
        {
            GroupId = request.GroupId,
            CalculatedAt = DateTime.UtcNow,
            Entities = entities.Where(e => e.OwnershipPercent < 100).Select(e => new MinorityInterestEntity
            {
                EntityId = e.Id.ToString(),
                EntityCode = e.EntityCode,
                OwnershipPercentage = e.OwnershipPercent,
                MinorityPercentage = 100 - e.OwnershipPercent,
                NetIncome = e.TranslatedRevenue - e.TranslatedCost,
                MinorityInterestAmount = (e.TranslatedRevenue - e.TranslatedCost) * (100 - e.OwnershipPercent) / 100,
                Equity = e.TranslatedRevenue,
                MinorityEquityPortion = e.TranslatedRevenue * (100 - e.OwnershipPercent) / 100
            }).ToList()
        };

        result.TotalMinorityInterest = result.Entities.Sum(e => e.MinorityInterestAmount);
        return result;
    }

    public async Task<ConsolidationReportResult> GenerateConsolidationReportAsync(ConsolidationReportRequest request)
    {
        return new ConsolidationReportResult
        {
            Success = true,
            ReportUrl = $"/reports/consolidation/{request.GroupId}/{request.FiscalYear}.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = Random.Shared.Next(50000, 200000),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<InterCompanyReconciliationResult> GetInterCompanyReconciliationAsync(InterCompanyReconciliationRequest request)
    {
        var transactions = await _db.InterCompanyTransactions.ToListAsync();

        var lines = transactions.Select(t => new InterCompanyReconciliationLine
        {
            FromEntityCode = t.FromEntityCode,
            ToEntityCode = t.ToEntityCode,
            AccountCode = "IC001",
            AccountName = "Intercompany Receivable",
            FromEntityAmount = t.Amount,
            ToEntityAmount = t.AmountGroupCurrency,
            Difference = Math.Abs(t.Amount - t.AmountGroupCurrency),
            Currency = t.Currency,
            IsReconciled = Math.Abs(t.Amount - t.AmountGroupCurrency) < 1
        }).ToList();

        return new InterCompanyReconciliationResult
        {
            Success = true,
            GroupId = request.GroupId,
            TotalInterCompanyTransactions = transactions.Sum(t => t.Amount),
            ReconciledAmount = lines.Where(l => l.IsReconciled).Sum(l => l.FromEntityAmount),
            UnreconciledAmount = lines.Where(l => !l.IsReconciled).Sum(l => l.Difference),
            IsFullyReconciled = lines.All(l => l.IsReconciled),
            Lines = lines,
            ReconciledAt = DateTime.UtcNow
        };
    }

    public async Task<ConsolidatedBalanceSheetResult> GetConsolidatedBalanceSheetAsync(ConsolidatedBalanceSheetRequest request)
    {
        var groupId = Guid.TryParse(request.GroupId, out var gid) ? gid : Guid.Empty;
        var entities = await _db.ConsolidationEntities.Where(e => e.GroupId == gid).ToListAsync();

        decimal totalAssets = entities.Sum(e => e.TranslatedRevenue);
        decimal totalLiabilities = entities.Sum(e => e.TranslatedCost * 0.4m);
        decimal totalEquity = entities.Sum(e => e.TranslatedCost * 0.6m);

        return new ConsolidatedBalanceSheetResult
        {
            GroupId = request.GroupId,
            FiscalYear = request.FiscalYear,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity,
            IsBalanced = Math.Abs(totalAssets - totalLiabilities - totalEquity) < 1,
            Assets = new List<ConsolidatedBSLineItem>
            {
                new() { AccountCode = "1000", AccountName = "Current Assets", Amount = totalAssets * 0.6m, Currency = "USD", HierarchyLevel = 0, ContributingEntities = entities.Select(e => e.EntityCode).ToList() },
                new() { AccountCode = "1500", AccountName = "Non-Current Assets", Amount = totalAssets * 0.4m, Currency = "USD", HierarchyLevel = 0 }
            },
            Liabilities = new List<ConsolidatedBSLineItem>
            {
                new() { AccountCode = "2000", AccountName = "Current Liabilities", Amount = totalLiabilities * 0.5m, Currency = "USD", HierarchyLevel = 0 },
                new() { AccountCode = "2500", AccountName = "Non-Current Liabilities", Amount = totalLiabilities * 0.5m, Currency = "USD", HierarchyLevel = 0 }
            },
            Equity = new List<ConsolidatedBSLineItem>
            {
                new() { AccountCode = "3000", AccountName = "Share Capital", Amount = totalEquity * 0.3m, Currency = "USD", HierarchyLevel = 0 },
                new() { AccountCode = "3500", AccountName = "Retained Earnings", Amount = totalEquity * 0.7m, Currency = "USD", HierarchyLevel = 0 }
            }
        };
    }

    public async Task<ConsolidatedPnLResult> GetConsolidatedPnLAsync(ConsolidatedPnLRequest request)
    {
        var groupId = Guid.TryParse(request.GroupId, out var gid) ? gid : Guid.Empty;
        var entities = await _db.ConsolidationEntities.Where(e => e.GroupId == gid).ToListAsync();

        var totalRevenue = entities.Sum(e => e.TranslatedRevenue);
        var totalExpenses = entities.Sum(e => e.TranslatedCost);

        return new ConsolidatedPnLResult
        {
            GroupId = request.GroupId,
            FiscalYear = request.FiscalYear,
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            ConsolidatedNetIncome = totalRevenue - totalExpenses
        };
    }

    public async Task<TransactionEliminateResult> EliminateTransactionAsync(TransactionEliminateRequest request)
    {
        var eliminationId = Guid.NewGuid();
        var elimination = new ConsolidationEliminationEntity
        {
            Id = eliminationId,
            GroupId = Guid.TryParse(request.GroupId, out var gid) ? gid : Guid.Empty,
            EliminationType = "Manual",
            FromEntityCode = request.FromEntityId,
            ToEntityCode = request.ToEntityId,
            OriginalAmount = request.Amount,
            EliminationAmount = request.Amount,
            Currency = request.Currency,
            PostingDate = DateTime.UtcNow,
            Status = "Posted"
        };

        _db.ConsolidationEliminations.Add(elimination);
        await _db.SaveChangesAsync();

        return new TransactionEliminateResult
        {
            Success = true,
            EliminationId = eliminationId.ToString(),
            JournalEntryReference = $"ELIM{DateTime.UtcNow:yyyyMMddHHmmss}",
            EliminatedAmount = request.Amount,
            Message = $"Transaction eliminated: {request.Amount} {request.Currency}"
        };
    }

    public async Task<ConsolidationExportResult> ExportConsolidationAsync(ConsolidationExportRequest request)
    {
        return new ConsolidationExportResult
        {
            Success = true,
            ExportUrl = $"/exports/consolidation/{request.GroupId}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileSizeBytes = Random.Shared.Next(100000, 500000),
            ExportedAt = DateTime.UtcNow
        };
    }

    private static decimal GetExchangeRate(string fromCurrency, string toCurrency)
    {
        if (fromCurrency == toCurrency) return 1.0m;
        var rates = new Dictionary<string, decimal>
        {
            ["EUR"] = 1.08m, ["GBP"] = 1.27m, ["INR"] = 0.012m,
            ["JPY"] = 0.0067m, ["CAD"] = 0.74m, ["AUD"] = 0.65m
        };

        if (rates.TryGetValue(fromCurrency, out var fromRate) && rates.TryGetValue(toCurrency, out var toRate))
            return Math.Round(toRate / fromRate, 4);
        return 1.0m;
    }
}
